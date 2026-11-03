
from kokoro import KPipeline
import soundfile as sf
import numpy as np
import io
from functools import lru_cache


# Load and cache one pipeline for each accent
@lru_cache(maxsize=2)
def get_pipeline(accent: str):
    return KPipeline(lang_code=accent)


def generate_audio(text: str, voice: str, accent: str):
    pipeline = get_pipeline(accent)

    generator = pipeline(text, voice=voice)

    audio_chunks = []

    for _, _, audio in generator:
        audio_chunks.append(audio)

    if not audio_chunks:
        raise ValueError("No audio was generated")

    audio = np.concatenate(audio_chunks)

    audio_buffer = io.BytesIO()
    sf.write(audio_buffer, audio, 24000, format="WAV")
    audio_buffer.seek(0)

    return audio_buffer
