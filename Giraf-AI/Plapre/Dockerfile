FROM pytorch/pytorch:2.10.0-cuda12.8-cudnn9-runtime

WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    git ffmpeg libsndfile1 python3-venv \
    && rm -rf /var/lib/apt/lists/*

RUN python -m venv /opt/venv

ENV PATH="/opt/venv/bin:$PATH"

# All Hugging Face downloads (model weights) go to the mounted models directory
ENV HF_HOME=/models/plapre

RUN python -m pip install --upgrade pip

RUN python -m pip install --no-cache-dir \
    "vllm>=0.15,<0.16" \
    "plapre[serve] @ git+https://github.com/syv-ai/plapre.git"

# Start script: converts a GPU memory budget in GiB (PLAPRE_GPU_GB) to the
# fraction that plapre-serve expects, then starts the server.
COPY start.sh /app/start.sh
RUN chmod +x /app/start.sh

ENV PLAPRE_GPU_GB=4

EXPOSE 8000

CMD ["/app/start.sh"]