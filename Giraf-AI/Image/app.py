import io, base64, os, time
from typing import Optional, List
import torch
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field
from diffusers import AutoPipelineForText2Image

app = FastAPI(title="Minimal Diffusers API")

MODELS_DIR = os.getenv("MODELS_DIR", "/models")
DEVICE = "cuda" if torch.cuda.is_available() else "cpu"
DTYPE = torch.float16 if DEVICE == "cuda" else torch.float32
DEFAULT_STEPS = int(os.getenv("DEFAULT_STEPS", "20"))
DEFAULT_GUIDANCE = float(os.getenv("DEFAULT_GUIDANCE", "7.0"))
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
pipe = AutoPipelineForText2Image.from_pretrained(
    MODEL_ID, torch_dtype=DTYPE, local_files_only=True
).to(DEVICE)
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
    generator = torch.Generator(device=DEVICE).manual_seed(req.seed) if req.seed is not None else None
    try:
        result = pipe(
            prompt=req.prompt,
            negative_prompt=req.negative_prompt,
            num_inference_steps=req.num_inference_steps or DEFAULT_STEPS,
            guidance_scale=req.guidance_scale if req.guidance_scale is not None else DEFAULT_GUIDANCE,
            width=width,
            height=height,
            num_images_per_prompt=req.n,
            generator=generator,
        )
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

    data = []
    for img in result.images:
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