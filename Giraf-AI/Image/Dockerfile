FROM nvidia/cuda:12.8.0-runtime-ubuntu22.04

ENV DEBIAN_FRONTEND=noninteractive
ENV PYTHONUNBUFFERED=1
ENV HF_HUB_OFFLINE=1
ENV TRANSFORMERS_OFFLINE=1
# Qwen-Image-2.1-Turbo is loaded with sequential CPU offload by default. This
# allocator setting also reduces fragmentation during repeated generations.
ENV CPU_OFFLOAD_MODE=sequential
ENV PYTORCH_CUDA_ALLOC_CONF=expandable_segments:True
ENV MAX_IMAGE_SIZE=1024

RUN apt-get update && apt-get install -y --no-install-recommends \
    git python3 python3-pip \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

RUN pip3 install --no-cache-dir --upgrade pip && \
    pip3 install --no-cache-dir torch torchvision --index-url https://download.pytorch.org/whl/cu128

COPY requirements.txt .
RUN pip3 install --no-cache-dir -r requirements.txt

COPY app.py .

EXPOSE 8000
CMD ["uvicorn", "app:app", "--host", "0.0.0.0", "--port", "8000", "--workers", "1"]