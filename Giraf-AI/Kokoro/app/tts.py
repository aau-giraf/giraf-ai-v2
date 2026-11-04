from kokoro import KPipeline
import soundfile as sf
import numpy as np
import io

def generate_audio(text, voice, accent):
    pipeline = KPipeline(lang_code=accent)
    chunks = []

    for _, _, audio in pipeline(text, voice=voice):
        chunks.append(audio)

    audio = np.concatenate(chunks)

    buffer = io.BytesIO()
    sf.write(buffer, audio, 24000, format="WAV")
    buffer.seek(0)

    return buffer