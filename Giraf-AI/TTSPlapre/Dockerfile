FROM pytorch/pytorch:2.10.0-cuda12.8-cudnn9-runtime

WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    git ffmpeg libsndfile1 python3-venv \
    && rm -rf /var/lib/apt/lists/*

RUN python -m venv /opt/venv

ENV PATH="/opt/venv/bin:$PATH"

RUN python -m pip install --upgrade pip

RUN python -m pip install --no-cache-dir \
    "vllm>=0.15,<0.16" \
    "plapre[serve] @ git+https://github.com/syv-ai/plapre.git"

EXPOSE 8000

CMD ["plapre-serve", "--checkpoint", "syvai/plapre-nano-v2", "--gpu-mem", "0.4", "--port", "8000"]