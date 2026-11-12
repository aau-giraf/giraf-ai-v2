# Kokoro TTS (Text-to-Speech)

A GPU-accelerated text-to-speech API running in Docker, based on [Kokoro-82M](https://huggingface.co/hexgrad/Kokoro-82M). The container downloads the Kokoro model itself on first start and stores it in your central models directory, so it is only downloaded once.

## Project structure

```
Kokoro/
├── Dockerfile
├── requirements.txt
├── README.md
└── app/
    ├── main.py    # FastAPI routes
    └── tts.py     # Kokoro pipelines and audio generation
```

## Prerequisites

- An NVIDIA GPU with an up-to-date driver (check with `nvidia-smi`)
- Docker with GPU support:
  - Windows: Docker Desktop with the WSL 2 backend
  - Linux: Docker Engine with the [NVIDIA Container Toolkit](https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/latest/install-guide.html)
- About 1 GB of free disk space for the model cache, plus several GB for the Docker image (it contains PyTorch with CUDA)
- Internet access when building the image (Python packages and the spaCy language model) and on the first start (model download). Afterwards no internet is needed, see [Offline mode](#offline-mode)

Python and a Hugging Face account are not needed on the host. 

## Model storage

All models live in one central directory on the host. Kokoro uses a `kokoro` subfolder of it:

```
<MODELS_DIR>/
├── other_model/
└── kokoro/                  # Kokoro cache (created automatically)
```

Pick a location and use the same one for every model, for example:

- Windows: `C:\Users\<you>\models`
- Linux: `$HOME/ubuntu/models` 

The folder is mounted into the container at `/models/kokoro`, and the Dockerfile sets `HF_HOME=/models/kokoro`, so everything Kokoro downloads at runtime (model weights and voice files) ends up in `<MODELS_DIR>/kokoro` on your machine.

The spaCy language model that Kokoro uses for text processing is small and is installed into the Docker image at build time, so it is not part of the model directory.

## Build the image

From the `Kokoro` folder (the one containing the `Dockerfile`). This needs internet access and takes a while, because the image contains PyTorch with CUDA:

```
docker build -t kokoro-tts .
```

## Run the container

Replace `<MODELS_DIR>` with your models directory. Port `8102` is used on the host because the Qwen image service uses `8000`; change it if needed.

**Windows (PowerShell)**

```powershell
docker run -d --gpus all -p 8102:8000 --name kokoro-server --restart unless-stopped `
    -v "<MODELS_DIR>\kokoro:/models/kokoro" `
    kokoro-tts
```

**Linux**

```bash
docker run -d --gpus all -p 8102:8000 --name kokoro-server --restart unless-stopped \
    -v "<MODELS_DIR>/kokoro:/models/kokoro" \
    kokoro-tts
```

## Wait until it is ready

On the first start the container downloads the model and all voices, which can take a few minutes. You can follow the progress with:

```
docker logs -f kokoro-server
```

Near the top of the log you should see the GPU that is used:

```
Using GPU: <your GPU name>
```

Kokoro runs on the GPU only. If no GPU is available to the container, it stops with an error instead of falling back to the CPU (see [Troubleshooting](#troubleshooting)).

The server is ready when you see:

```
Kokoro ready.
```

The API only accepts requests after this point, so `/health` will not respond while the model is still loading. Later starts take a few seconds because everything is read from the cache.

To confirm the model was stored in the right place, check that `<MODELS_DIR>/kokoro/hub` exists and contains files.

## Health check

```
curl http://localhost:8001/health
```

Expected response:

```json
{"status": "healthy"}
```

## Usage

### Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/health` | Health check |
| GET | `/v1/models` | Lists the available model |
| POST | `/v1/audio/speech` | Generates speech and returns a WAV file |

### Request parameters (`POST /v1/audio/speech`)

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `input` | string | required | The text to speak (must not be empty) |
| `accent` | `"a"` or `"b"` | `"a"` | `a` = American English, `b` = British English |
| `voice` | `"f"` or `"m"` | `"f"` | `f` = female, `m` = male |

| Accent | Voice | Kokoro voice |
|--------|-------|--------------|
| `a` | `f` | `af_heart` |
| `a` | `m` | `am_adam` |
| `b` | `f` | `bf_emma` |
| `b` | `m` | `bm_george` |

Invalid values for `accent` or `voice` return `422`, and text that produces no audio returns `400`.

### Example: test the setup by generating a WAV file
The output will be stored within the directory that you are requesting from.

**Windows (PowerShell)**

```powershell
Invoke-RestMethod -Uri http://localhost:8102/v1/audio/speech `
    -Method Post `
    -ContentType "application/json" `
    -Body '{"input": "Hello, this is a test.", "accent": "a", "voice": "f"}' `
    -OutFile output.wav
```

**Linux / macOS (or `curl.exe` on Windows)**

```bash
curl -X POST http://localhost:8102/v1/audio/speech \
    -H "Content-Type: application/json" \
    -d '{"input": "Hello, this is a test.", "accent": "a", "voice": "f"}' \
    --output output.wav
```

## Offline mode

Once the image is built and the first start has finished, everything is available locally (the spaCy model in the image, the Kokoro model and voices in the model directory), and the container no longer needs internet access. To enforce this, add `-e HF_HUB_OFFLINE=1` to the `docker run` command. Only do this after you have seen `Kokoro ready.` once, because the model and all voices must already be in the cache.

## Access from another device

Find the IP address of the host:

- Windows: `ipconfig`
- Linux: `hostname -I`
- macOS: `ipconfig getifaddr en0`

Then call `http://<host-ip>:8102/v1/audio/speech` from the other device. You may need to allow port `8102` through the host firewall.

## Managing the container

```
docker stop kokoro-server       # stop
docker start kokoro-server      # start again
docker logs -f kokoro-server    # view logs
```

### Rebuild after code changes

```
docker rm -f kokoro-server
docker build -t kokoro-tts .
```

Then run the `docker run` command again. The model is **not** downloaded again, because it is stored in `<MODELS_DIR>/kokoro` on the host.

## Configuration

| Setting | How | Description |
|---------|-----|-------------|
| GPU access | `--gpus all` | Gives the container access to the NVIDIA GPU |
| Host port | `-p <host-port>:8000` | Port the API is reachable on |
| Model directory | `-v "<MODELS_DIR>\kokoro:/models/kokoro"` | Where the model cache is stored |
| Offline mode | `-e HF_HUB_OFFLINE=1` | Never contact Hugging Face (cache must be complete) |
| Cache path in container | `HF_HOME` (set in the Dockerfile) | Do not change unless you also change the mount |

## Troubleshooting

**The container stops with `No CUDA GPU available`**
The container cannot see the GPU. Because `--restart unless-stopped` is set, it will keep restarting until this is fixed. Check that:
- `--gpus all` is in the `docker run` command
- `nvidia-smi` works on the host
- Docker Desktop uses the WSL 2 backend (Windows) or the NVIDIA Container Toolkit is installed and Docker was restarted (Linux)
- `docker exec kokoro-server nvidia-smi` lists your GPU

**`CUDA error` or driver version errors on start**
The CUDA version in the Dockerfile (`cu126`) needs a recent enough NVIDIA driver. Update the driver, or change `cu126` in the Dockerfile to a version your driver supports and rebuild.

**Out of memory when running together with the Qwen image service**
Both services share the same GPU memory. Kokoro keeps its model in VRAM for as long as it runs and is never moved off the GPU. If there is not enough free VRAM when Kokoro starts, it fails with a CUDA out-of-memory error. Start Kokoro first so it reserves its share before Qwen loads, and check actual usage with `nvidia-smi`.

**`/health` does not respond right after starting**
The model is probably still downloading or loading. Check `docker logs -f kokoro-server` and wait for `Kokoro ready.`

**The model is downloaded again after every rebuild**
The volume is missing or points to the wrong path. Check that the `-v` flag is in the `docker run` command and that `<MODELS_DIR>/kokoro/hub` contains files after the first start.

**Port is already in use**
Another container (for example the Qwen service) is using the port. Change the host side of `-p`, for example `-p 8002:8000`.

**Build fails while installing packages or the spaCy model**
The build needs internet access. Check your connection and run `docker build -t kokoro-tts .` again.

**Download fails on the first start**
The container needs internet access on the first start. Check your connection and run `docker restart kokoro-server`. Files that were already downloaded are kept.

**Errors with `HF_HUB_OFFLINE=1`**
The cache is incomplete. Remove the flag, restart the container, wait for `Kokoro ready.` and then add it again.

**`422 Unprocessable Entity`**
The request body is invalid. Check that `input` is a non-empty string, `accent` is `a` or `b`, and `voice` is `f` or `m`.

## How it works

1. The spaCy language model is installed in the image at build time. On startup the app checks that a CUDA GPU is available, loads one Kokoro pipeline per accent on it and generates a short clip with every voice. This downloads the model and voices into the cache (`HF_HOME`) the first time.
2. Each request reuses the loaded pipeline, generates the audio in chunks, joins them and returns a 24 kHz WAV file.
3. Requests are processed one at a time, which keeps GPU memory use predictable.