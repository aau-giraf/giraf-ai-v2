from fastapi import FastAPI
from fastapi.responses import StreamingResponse
from pydantic import BaseModel
from app.tts import generate_audio
import uuid

app = FastAPI()

class TTSRequest(BaseModel):
    input: str
    accent: str = "a"
    voice: str = "f"


@app.get("/health")
def health():
    return {"status": "healthy"}


@app.get("/v1/models")
def models():
    return {"data": ["kokoro-82m"]}


@app.post("/v1/audio/speech")
def speech(request: TTSRequest):
    voices = {
        "a": {"f": "af_heart", "m": "am_adam"},
        "b": {"f": "bf_emma", "m": "bm_george"}
    }

    voice = voices[request.accent][request.voice]
    audio = generate_audio(request.input, voice, request.accent)

    filename = f"{uuid.uuid4()}.wav"

    return StreamingResponse(
        audio,
        media_type="audio/wav",
        headers={"Content-Disposition": f'attachment; filename="{filename}"'}
    )