import base64, io, os, threading, time
from typing import List, Optional

import torch
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field

# If your image imports QwenImage21Pipeline from somewhere else, keep that import.
from diffusers import QwenImage21Pipeline

app = FastAPI(title="Minimal Diffusers API")

# ---------------------------------------------------------------- settings --
MODEL_PATH = os.getenv("MODEL_PATH", "/models/qwen-image-2.1-turbo")
DEVICE = "cuda"
DTYPE = torch.bfloat16  # fp16 can give black/garbled images with Qwen models

_steps = os.getenv("DEFAULT_STEPS")
DEFAULT_STEPS = int(_steps) if _steps else None  # unset = model's built-in 8 steps
DEFAULT_GUIDANCE = float(os.getenv("DEFAULT_GUIDANCE", "1.0"))
MAX_IMAGE_SIZE = int(os.getenv("MAX_IMAGE_SIZE", "1024"))

# split: transformer permanently in VRAM, text encoder in RAM, VAE per VAE_DEVICE
# model: whole components swapped into VRAM only while they run (fallback)
CPU_OFFLOAD_MODE = os.getenv("CPU_OFFLOAD_MODE", "split").strip().lower()
# split only: cpu | cuda | cuda_on_demand (VAE visits the GPU only while decoding)
VAE_DEVICE = os.getenv("VAE_DEVICE", "cpu").strip().lower()
# Tiled VAE decoding saves memory but can leave visible seams/lines in the image.
VAE_TILING = os.getenv("VAE_TILING", "1") == "1"
# split only: cpu = encoder runs on the CPU; stream = encoder weights stay in RAM
# but its blocks are streamed through the GPU while it runs (faster, a few GB VRAM)
TEXT_ENCODER_MODE = os.getenv("TEXT_ENCODER_MODE", "cpu").strip().lower()
# Overlaps copying with compute, but pins a second copy of the encoder (~17 GB) in RAM
ENCODER_USE_STREAM = os.getenv("ENCODER_USE_STREAM", "0") == "1"
# TEXT_ENCODER_MODE=cpu only: dtype of the text encoder on the CPU. CPUs without
# native bf16 (e.g. Intel Ice Lake) multiply float32 ~4x faster, but float32
# doubles the encoder's RAM (17 GB -> ~34 GB).
ENCODER_DTYPE = os.getenv("ENCODER_DTYPE", "bfloat16").strip().lower()

os.environ["HF_HUB_OFFLINE"] = "1"
os.environ["TRANSFORMERS_OFFLINE"] = "1"

if not torch.cuda.is_available():
    raise RuntimeError("A CUDA GPU is required")
if CPU_OFFLOAD_MODE not in ("split", "model"):
    raise RuntimeError(f"CPU_OFFLOAD_MODE must be split or model, got {CPU_OFFLOAD_MODE!r}")
if VAE_DEVICE not in ("cpu", "cuda", "cuda_on_demand"):
    raise RuntimeError(f"VAE_DEVICE must be cpu, cuda or cuda_on_demand, got {VAE_DEVICE!r}")
if TEXT_ENCODER_MODE not in ("cpu", "stream"):
    raise RuntimeError(f"TEXT_ENCODER_MODE must be cpu or stream, got {TEXT_ENCODER_MODE!r}")
if ENCODER_DTYPE not in ("bfloat16", "float32"):
    raise RuntimeError(f"ENCODER_DTYPE must be bfloat16 or float32, got {ENCODER_DTYPE!r}")
if ENCODER_DTYPE == "float32" and TEXT_ENCODER_MODE == "stream":
    raise RuntimeError("ENCODER_DTYPE=float32 only works with TEXT_ENCODER_MODE=cpu")

# ------------------------------------------------------------ model loading --
print(f"Loading {MODEL_PATH} (mode={CPU_OFFLOAD_MODE}, vae={VAE_DEVICE})...")
pipe = QwenImage21Pipeline.from_pretrained(  # loads on the CPU first
    MODEL_PATH, dtype=DTYPE, local_files_only=True
)

if CPU_OFFLOAD_MODE == "model":
    pipe.enable_model_cpu_offload()

else:  # split
    pipe.transformer.to(DEVICE)

    # Create latents etc. on the GPU, not where the text encoder lives.
    type(pipe)._execution_device = property(lambda self: torch.device(DEVICE))

    def _move(obj, device):
        """Recursively move tensors (also inside dicts / model outputs)."""
        if torch.is_tensor(obj):
            return obj.to(device)
        if isinstance(obj, dict):  # transformers' ModelOutput is a dict subclass
            for k in list(obj.keys()):
                obj[k] = _move(obj[k], device)
            return obj
        if isinstance(obj, (list, tuple)):
            return type(obj)(_move(v, device) for v in obj)
        return obj

    _t = {}

    if TEXT_ENCODER_MODE == "stream":
        # Weights stay in RAM; each layer (leaf module) is copied to the GPU just
        # before it runs and released right after, so VRAM use stays tiny. Inputs
        # and outputs are already on the GPU, so the hooks below only measure time.
        # (block_level does not work here: it only splits direct ModuleList
        # children, and this encoder is nested, so it would load everything at once.)
        from diffusers.hooks import apply_group_offloading

        apply_group_offloading(
            pipe.text_encoder,
            onload_device=torch.device(DEVICE),
            offload_device=torch.device("cpu"),
            offload_type="leaf_level",
            use_stream=ENCODER_USE_STREAM,
        )

        def _enc_pre(mod, args, kwargs):
            _t["enc"] = time.time()

        def _enc_post(mod, args, kwargs, out):
            print(f"[timing] text encoder (streamed): {time.time() - _t['enc']:.1f}s", flush=True)

    else:
        # The pipeline tokenizes onto the GPU: send the inputs to the CPU text
        # encoder, and its outputs straight back (the pipeline indexes them with
        # GPU tensors).
        def _enc_pre(mod, args, kwargs):
            _t["enc"] = time.time()
            return _move(args, "cpu"), _move(kwargs, "cpu")

        def _enc_post(mod, args, kwargs, out):
            print(f"[timing] text encoder (cpu): {time.time() - _t['enc']:.1f}s", flush=True)
            return _move(out, DEVICE)

        if ENCODER_DTYPE == "float32":
            pipe.text_encoder.to(torch.float32)

            # The pipeline casts the text embeddings to the encoder's dtype
            # (float32), but the transformer is bfloat16: cast them back right
            # at the transformer's input.
            def _transformer_pre(mod, args, kwargs):
                kwargs = {
                    k: v.to(DTYPE)
                    if torch.is_tensor(v) and v.dtype == torch.float32 and v.ndim == 3
                    else v
                    for k, v in kwargs.items()
                }
                return args, kwargs

            pipe.transformer.register_forward_pre_hook(_transformer_pre, with_kwargs=True)

    pipe.text_encoder.register_forward_pre_hook(_enc_pre, with_kwargs=True)
    pipe.text_encoder.register_forward_hook(_enc_post, with_kwargs=True)

    # VAE: decode on the CPU, on the GPU, or on the GPU only while decoding.
    # Weights loaded from disk can stay file-backed (page cache). Under RAM
    # pressure the OS evicts those pages and re-reads them from a slow disk the
    # next time the VAE is used. Copying them into normal RAM once prevents that.
    for _p in list(pipe.vae.parameters()) + list(pipe.vae.buffers()):
        _p.data = _p.data.clone()
    if VAE_DEVICE == "cuda":
        pipe.vae.to(DEVICE)
    _orig_decode = pipe.vae.decode

    def _vae_decode(z, *a, **kw):
        t0 = time.time()
        on_demand = VAE_DEVICE == "cuda_on_demand"
        if on_demand:
            pipe.vae.to(DEVICE)
        try:
            return _orig_decode(z.to("cpu" if VAE_DEVICE == "cpu" else DEVICE), *a, **kw)
        finally:
            if on_demand:
                pipe.vae.to("cpu")
                torch.cuda.empty_cache()
            print(f"[timing] vae decode ({VAE_DEVICE}): {time.time() - t0:.1f}s", flush=True)

    pipe.vae.decode = _vae_decode

pipe.vae.enable_slicing()
if VAE_TILING:
    pipe.vae.enable_tiling()


def component_devices():
    """Which device each component's weights are on (also shown in /health)."""
    return {
        n: str(next(c.parameters()).device)
        for n, c in pipe.components.items()
        if isinstance(c, torch.nn.Module) and any(True for _ in c.parameters())
    }


print("Component devices:", component_devices())
print("Model ready.")
print("Note: negative_prompt only takes effect when guidance_scale is above 1.0; "
      "at 1.0 (the default) it is ignored.")

generation_lock = threading.Lock()  # one generation at a time (protects VRAM)


# ------------------------------------------------------------------- schema --
class ImageGenerationRequest(BaseModel):
    prompt: str
    negative_prompt: Optional[str] = None
    n: int = Field(1, ge=1, le=4)
    size: str = "1024x1024"
    num_inference_steps: Optional[int] = None
    guidance_scale: Optional[float] = None  # passed on as true_cfg_scale
    seed: Optional[int] = None


class ImageData(BaseModel):
    b64_json: str


class ImageGenerationResponse(BaseModel):
    created: int
    data: List[ImageData]


def parse_size(size: str):
    try:
        w, h = map(int, size.lower().split("x"))
        return max(256, (w // 8) * 8), max(256, (h // 8) * 8)
    except Exception:
        return 1024, 1024  # wrong format -> default 1024x1024


# ---------------------------------------------------------------- endpoints --
# Plain "def" so FastAPI runs it in a worker thread and /health stays responsive.
@app.post("/v1/images/generations", response_model=ImageGenerationResponse)
def generate_images(req: ImageGenerationRequest):
    width, height = parse_size(req.size)
    if width > MAX_IMAGE_SIZE or height > MAX_IMAGE_SIZE:
        raise HTTPException(
            status_code=400,
            detail=f"Image dimensions must be at most {MAX_IMAGE_SIZE}x{MAX_IMAGE_SIZE}",
        )

    generator = (
        None
        if req.seed is None or req.seed == -1
        else torch.Generator(device=DEVICE).manual_seed(req.seed)
    )
    kwargs = dict(
        prompt=req.prompt,
        true_cfg_scale=(
            req.guidance_scale if req.guidance_scale is not None else DEFAULT_GUIDANCE
        ),
        width=width,
        height=height,
        num_images_per_prompt=1,
        generator=generator,
    )
    steps = req.num_inference_steps or DEFAULT_STEPS
    if steps:
        kwargs["num_inference_steps"] = steps
    # Only pass the negative prompt when guidance is above 1: otherwise it has no
    # effect, and diffusers would log a warning on every request.
    if req.negative_prompt and kwargs["true_cfg_scale"] > 1:
        kwargs["negative_prompt"] = req.negative_prompt

    try:
        images = []
        with generation_lock, torch.inference_mode():
            for _ in range(req.n):  # one at a time to keep peak VRAM low
                images.append(pipe(**kwargs).images[0])
                torch.cuda.empty_cache()
    except Exception as e:
        import traceback

        traceback.print_exc()  # full traceback in the container log
        raise HTTPException(status_code=500, detail=f"{type(e).__name__}: {e}")

    data = []
    for img in images:
        buf = io.BytesIO()
        img.save(buf, format="PNG")
        data.append(ImageData(b64_json=base64.b64encode(buf.getvalue()).decode()))
    return ImageGenerationResponse(created=int(time.time()), data=data)


@app.get("/health")
def health():
    return {
        "status": "ok",
        "model": MODEL_PATH,
        "offload_mode": CPU_OFFLOAD_MODE,
        "components": component_devices(),
    }


@app.get("/v1/models")
def list_models():
    return {"object": "list", "data": [{"id": MODEL_PATH, "object": "model", "owned_by": "local"}]}