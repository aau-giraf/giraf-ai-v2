import io, base64, os, time
from typing import Optional, List
import torch
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field
from diffusers import QwenImage21Pipeline

app = FastAPI(title="Minimal Diffusers API")

MODELS_DIR = os.getenv("MODELS_DIR", "/models")
DEVICE = "cuda" if torch.cuda.is_available() else "cpu"
DTYPE = torch.bfloat16 if DEVICE == "cuda" else torch.float32
DEFAULT_STEPS = int(os.getenv("DEFAULT_STEPS", "8"))
DEFAULT_GUIDANCE = float(os.getenv("DEFAULT_GUIDANCE", "1.0"))
CPU_OFFLOAD_MODE = os.getenv("CPU_OFFLOAD_MODE", "sequential").lower()
MAX_IMAGE_SIZE = int(os.getenv("MAX_IMAGE_SIZE", "1024"))
os.environ["HF_HUB_OFFLINE"] = "1"
os.environ["TRANSFORMERS_OFFLINE"] = "1"

# Auto-detect the single model folder
MODEL_ID = next(
    (os.path.join(MODELS_DIR, n) for n in os.listdir(MODELS_DIR)
     if os.path.isfile(os.path.join(MODELS_DIR, n, "model_index.json"))),
    None,
)
if not MODEL_ID:
    raise RuntimeError(f"No Diffusers model found in {MODELS_DIR}")

print(f"Loading {MODEL_ID} on {DEVICE}...")
pipe = QwenImage21Pipeline.from_pretrained(
    MODEL_ID, dtype=DTYPE, local_files_only=True
)

if DEVICE == "cuda":
    if CPU_OFFLOAD_MODE == "sequential":
        # Keep only the currently executing module on the GPU. This is slower,
        # but keeps Qwen-Image-2.1-Turbo within GPUs with less than 20 GB VRAM.
        pipe.enable_sequential_cpu_offload()
    elif CPU_OFFLOAD_MODE == "model":
        pipe.enable_model_cpu_offload()
    elif CPU_OFFLOAD_MODE == "none":
        pipe.to(DEVICE)
    else:
        raise RuntimeError(
            "CPU_OFFLOAD_MODE must be one of: sequential, model, none"
        )

    pipe.vae.enable_slicing()
    pipe.vae.enable_tiling()
else:
    pipe.to(DEVICE)
print("Model ready.")

class ImageGenerationRequest(BaseModel):
    prompt: str
    negative_prompt: Optional[str] = None
    n: int = Field(1, ge=1, le=4)
    size: str = "1024x1024"
    num_inference_steps: Optional[int] = None
    guidance_scale: Optional[float] = None
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
        return 1024, 1024 # If the size in in the wrong format return default 1024x1024

@app.post("/v1/images/generations", response_model=ImageGenerationResponse)
async def generate_images(req: ImageGenerationRequest):
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
    try:
        images = []
        # Generate requests one at a time. A batch of four 1024px images can
        # exceed the memory budget even when a single image fits comfortably.
        with torch.inference_mode():
            for _ in range(req.n):
                result = pipe(
                    prompt=req.prompt,
                    negative_prompt=req.negative_prompt or " ",
                    num_inference_steps=req.num_inference_steps or DEFAULT_STEPS,
                    true_cfg_scale=(
                        req.guidance_scale
                        if req.guidance_scale is not None
                        else DEFAULT_GUIDANCE
                    ),
                    width=width,
                    height=height,
                    num_images_per_prompt=1,
                    generator=generator,
                )
                images.append(result.images[0])
                if DEVICE == "cuda":
                    del result
                    torch.cuda.empty_cache()
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

    data = []
    for img in images:
        buf = io.BytesIO()
        img.save(buf, format="PNG")
        data.append(ImageData(b64_json=base64.b64encode(buf.getvalue()).decode()))
    return ImageGenerationResponse(created=int(time.time()), data=data)

@app.get("/health")
def health():
    return {"status": "ok", "model": MODEL_ID, "device": DEVICE}

@app.get("/v1/models")
def list_models():
    return {"object": "list", "data": [{"id": MODEL_ID, "object": "model", "owned_by": "local"}]}