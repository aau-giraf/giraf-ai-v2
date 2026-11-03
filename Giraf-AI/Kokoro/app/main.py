
from fastapi import FastAPI, HTTPException
from fastapi.responses import StreamingResponse
from pydantic import BaseModel
from typing import Literal
import re

from app.tts import generate_audio

app = FastAPI(title="Kokoro TTS API")


class TTSRequest(BaseModel):
    text: str
    accent: Literal["a", "b"] = "a"
    voice: Literal["f", "m"] = "f"


# Voice selection based on accent and gender
VOICES = {
    "a": {
        "f": "af_heart",
        "m": "am_adam"
    },
    "b": {
        "f": "bf_emma",
        "m": "bm_george"
    }
}


def create_filename(text: str):
    filename = re.sub(r"[^a-zA-Z0-9]+", "-", text.lower())
    filename = filename.strip("-")[:50]

    if not filename:
        filename = "speech"

    return f"{filename}.wav"


@app.get("/health")
def health():
    return {"status": "healthy"}


@app.post("/tts")
def text_to_speech(request: TTSRequest):

    if not request.text.strip():
        raise HTTPException(
            status_code=400,
            detail="Text cannot be empty"
        )

    voice = VOICES[request.accent][request.voice]

    try:
        audio = generate_audio(
            request.text,
            voice,
            request.accent
        )

        filename = create_filename(request.text)

        return StreamingResponse(
            audio,
            media_type="audio/wav",
            headers={
                "Content-Disposition": f'attachment; filename="{filename}"'
            }
        )

    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))
