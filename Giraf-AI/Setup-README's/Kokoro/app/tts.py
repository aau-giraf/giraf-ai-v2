import io
import threading

import numpy as np
import soundfile as sf
import torch
from kokoro import KPipeline

SAMPLE_RATE = 24000

# Kokoro runs on the GPU only
DEVICE = "cuda"

# accent -> gender -> Kokoro voice name
VOICES = {
    "a": {"f": "af_heart", "m": "am_adam"},    # American English
    "b": {"f": "bf_emma", "m": "bm_george"},   # British English
}

_pipelines: dict[str, KPipeline] = {}
_pipelines_lock = threading.Lock()
_inference_lock = threading.Lock()


def get_pipeline(accent: str) -> KPipeline:
    """Return the cached pipeline for an accent, creating it on first use."""
    with _pipelines_lock:
        if accent not in _pipelines:
            _pipelines[accent] = KPipeline(lang_code=accent, device=DEVICE)
        return _pipelines[accent]


def warmup() -> None:
    """Load every pipeline and generate a short clip with every voice.

    This forces the model weights and all voice files to be downloaded into
    the Hugging Face cache (HF_HOME) at startup instead of on the first request.
    """
    if not torch.cuda.is_available():
        raise RuntimeError(
            "No CUDA GPU available. Start the container with --gpus all and check "
            "that the NVIDIA driver and Docker GPU support are installed."
        )
    print(f"Using GPU: {torch.cuda.get_device_name(0)}", flush=True)

    for accent, genders in VOICES.items():
        pipeline = get_pipeline(accent)
        for voice in genders.values():
            print(f"Warming up accent={accent} voice={voice}", flush=True)
            for _ in pipeline("Hello.", voice=voice):
                pass


def generate_audio(text: str, voice: str, accent: str) -> io.BytesIO:
    pipeline = get_pipeline(accent)
    chunks = []

    # Serialise inference: the model is not guaranteed to be thread-safe.
    with _inference_lock:
        for _, _, audio in pipeline(text, voice=voice):
            chunks.append(audio)

    if not chunks:
        raise ValueError("No audio could be generated from the given text.")

    audio = np.concatenate(chunks)

    buffer = io.BytesIO()
    sf.write(buffer, audio, SAMPLE_RATE, format="WAV")
    buffer.seek(0)

    return buffer