import uuid
from contextlib import asynccontextmanager
from typing import Literal

from fastapi import FastAPI, HTTPException
from fastapi.responses import StreamingResponse
from pydantic import BaseModel, Field

from app.tts import VOICES, generate_audio, warmup


@asynccontextmanager
async def lifespan(app: FastAPI):
    # Runs before the server accepts requests, so /health only answers
    # once the model and all voices are downloaded and loaded.
    print("Loading Kokoro (first start downloads the model, this can take a few minutes)...", flush=True)
    warmup()
    print("Kokoro ready.", flush=True)
    yield


app = FastAPI(lifespan=lifespan)


class TTSRequest(BaseModel):
    input: str = Field(min_length=1)
    accent: Literal["a", "b"] = "a"   # a = American, b = British
    voice: Literal["f", "m"] = "f"    # f = female, m = male


@app.get("/health")
def health():
    return {"status": "healthy"}


@app.get("/v1/models")
def models():
    return {"data": ["kokoro-82m"]}


@app.post("/v1/audio/speech")
def speech(request: TTSRequest):
    voice = VOICES[request.accent][request.voice]

    try:
        audio = generate_audio(request.input, voice, request.accent)
    except ValueError as e:
        raise HTTPException(status_code=400, detail=str(e))

    filename = f"{uuid.uuid4()}.wav"

    return StreamingResponse(
        audio,
        media_type="audio/wav",
        headers={"Content-Disposition": f'attachment; filename="{filename}"'},
    )