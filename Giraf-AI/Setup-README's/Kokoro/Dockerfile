FROM python:3.12-slim

WORKDIR /app

# Install PyTorch with CUDA support (the pip wheels bundle the CUDA runtime,
# so no NVIDIA base image is needed; the host only needs the NVIDIA driver)
RUN pip install --no-cache-dir \
    --index-url https://download.pytorch.org/whl/cu126 \
    torch

# Install application dependencies
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Bake the spaCy model used by Kokoro's text processing into the image,
# so it is not downloaded again every time a container is created
RUN python -m spacy download en_core_web_sm

# Store the Hugging Face cache in /models/kokoro so it can be mounted
# from the central models directory on the host
ENV HF_HOME=/models/kokoro
RUN mkdir -p /models/kokoro

# Copy application
COPY . .

EXPOSE 8000

# Start API server
CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000"]