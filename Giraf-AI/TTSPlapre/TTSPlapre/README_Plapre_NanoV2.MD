# Plapre-nano-v2 Docker Guide

## Technical Requirements

- Windows 11
- NVIDIA GPU
- Docker Desktop with NVIDIA GPU support
- Hugging Face access token for [Plapre-nano-v2](https://huggingface.co/syvai/plapre-nano-v2)

## 1. Create the Project Files

Create the following files in your project directory:

```text
PlapreNanoV2/
├── Dockerfile
├── compose.yaml
├── .env
├── convert-audio.ps1
└── output/
```

### Dockerfile

```dockerfile
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
```

### compose.yaml

```yaml
services:
  plapre:
    build: .
    container_name: plapre
    ports:
      - "8000:8000"
    environment:
      HF_TOKEN: ${HF_TOKEN}
      HF_HOME: /root/.cache/huggingface
    volumes:
      - huggingface_cache:/root/.cache/huggingface
      - ./output:/output
    deploy:
      resources:
        reservations:
          devices:
            - driver: nvidia
              count: all
              capabilities: [gpu]
    restart: unless-stopped

volumes:
  huggingface_cache:
```

### .env

Create a Hugging Face access token with **Read** permissions and replace `YOUR_HUGGINGFACE_TOKEN` with your Hugging Face access token.

```dotenv
HF_TOKEN=YOUR_HUGGINGFACE_TOKEN
```

Make sure the token has access to the [Plapre-nano-v2 model](https://huggingface.co/syvai/plapre-nano-v2).

**Important:** Keep your `.env` file private. Do not commit your access token to a public Git repository.

### convert-audio.ps1

This script converts the generated PCM audio into a WAV file and saves it in your chosen output directory. Each run creates a new file with a timestamp in its name.

Replace `YOUR_DIRECTORY` with your desired output directory, for example:

`C:\Users\alexa\RiderProjects\PlapreNanoV2\output`

```powershell
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$outputFile = "speech_$timestamp.wav"
$outputDir = "YOUR_DIRECTORY"

New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

docker run --rm `
    -v "${PWD}:/audio" `
    -v "${outputDir}:/output" `
    linuxserver/ffmpeg `
    -y `
    -f s16le -ar 24000 -ac 1 `
    -i /audio/speech.pcm `
    -c:a pcm_s16le -f wav `
    "/output/$outputFile"

Write-Host "Saved audio as: $outputDir\$outputFile"
```

The script expects the input file to be named `speech.pcm` in the current working directory.

## 2. Build and Start the Docker Container

Run the following commands in PowerShell from your project directory.

### Verify NVIDIA GPU Access

```powershell
docker run --rm --gpus all `
  nvidia/cuda:12.8.1-base-ubuntu24.04 nvidia-smi
```

Verify that your NVIDIA GPU appears in the output before continuing.

### Build the Project

```powershell
docker compose build
```

The first build may take some time because Docker needs to download the base image and install the Python dependencies.

### Start the Project

```powershell
docker compose up -d
```

### View the Logs

```powershell
docker compose logs -f
```

Wait for the model to load and the server to start. The first startup may take longer because the model needs to be downloaded from Hugging Face.

Press `Ctrl+C` to stop following the logs. This does not stop the container.

## 3. Check That the Server Is Working

Run these commands in PowerShell.

### Check Container Status

```powershell
docker compose ps
```

### View the Last 100 Log Lines

```powershell
docker compose logs --tail 100 plapre
```

### Check Server Health

```powershell
Invoke-RestMethod http://localhost:8000/health
```

### List Available Speakers

```powershell
Invoke-RestMethod http://localhost:8000/v1/speakers
```

If these endpoints return valid responses, the server is responding to requests. If an endpoint fails, inspect the container logs for errors.

## 4. Generate Speech Using an HTTP Request

Run the following PowerShell commands to generate speech.

### Send the Request

```powershell
$body = @{
    text = "Dette er en test. 1 2 3, 3 2 1"
    speaker = "tor"
    temperature = 0.8
    top_p = 0.95
    top_k = 50
    max_tokens = 500
} | ConvertTo-Json

Invoke-WebRequest `
    -Uri "http://localhost:8000/v1/audio/speech" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body `
    -OutFile "speech.pcm"
```

The response is saved as `speech.pcm` in the current working directory.

### Convert PCM to WAV

```powershell
.\convert-audio.ps1
```

The script converts `speech.pcm` into a WAV file and saves it in the output directory configured in `convert-audio.ps1`.

Each conversion receives a timestamped filename, for example:

```text
speech_2026-10-29_10-30-00.wav
```

## 5. Useful Docker Commands

### Stop the Container

```powershell
docker compose down
```

### Start the Container Again

```powershell
docker compose up -d
```

### Restart the Container

```powershell
docker compose restart
```

### Follow the Logs

```powershell
docker compose logs -f plapre
```

### Rebuild After Changing the Dockerfile

```powershell
docker compose up -d --build
```

### Check GPU Usage

```powershell
nvidia-smi
```

This checks GPU usage on the Windows host, provided the NVIDIA driver tools are available.

---

## Troubleshooting

- **GPU not detected:** Verify NVIDIA driver installation and Docker Desktop GPU support.
- **Hugging Face authentication errors:** Check that `HF_TOKEN` is correct and that your account has access to the model.
- **Server not responding:** Check `docker compose logs --tail 100 plapre` and wait for model loading to finish.
- **Out of GPU memory:** Check GPU usage and consider reducing the model's GPU memory allocation if supported by your setup.
- **WAV conversion fails:** Verify that Docker can pull `linuxserver/ffmpeg`, that `speech.pcm` exists in the current directory, and that the output directory is accessible.
