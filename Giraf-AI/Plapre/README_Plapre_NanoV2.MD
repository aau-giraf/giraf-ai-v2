# Plapre-nano-v2 TTS (Text-to-Speech)

A GPU-accelerated Danish text-to-speech API running in Docker, based on [Plapre-nano-v2](https://huggingface.co/syvai/plapre-nano-v2). The container downloads the model itself on first start and stores it in your central models directory, so it is only downloaded once.

This guide is written for an Ubuntu server with bash. `sudo` is needed for `docker` unless your user is in the `docker` group.

## Project structure

```bash
Plapre/
├── Dockerfile
├── start.sh
└── README_Plapre_NanoV2.MD
```

## How it works

1. The Dockerfile installs vLLM and the `plapre` package into a virtual environment in the image, and adds `start.sh`.
2. On start the container runs `start.sh`, which converts the GiB budget to a fraction of the card's VRAM and starts `plapre-serve`. The server downloads the model and the Kanade audio codec into `HF_HOME` (the mounted models directory) the first time and loads them onto the GPU. Nothing is downloaded at build time.
3. Each request to `/v1/audio/speech` returns raw 24 kHz PCM audio, which you can convert to WAV with `ffmpeg`.

## Prerequisites

- An NVIDIA GPU with an up-to-date driver (check with `nvidia-smi`)
- Docker Engine with the [NVIDIA Container Toolkit](https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/latest/install-guide.html)
- No Hugging Face account or token is needed. [Plapre-nano-v2](https://huggingface.co/syvai/plapre-nano-v2) and the audio codec it uses ([Kanade](https://huggingface.co/frothywater/kanade-25hz-clean)) are public
- A few GB of free disk space for the model cache, plus several GB for the Docker image (it contains PyTorch and vLLM)
- Internet access when building the image and on the first start (model download). Afterwards no internet is needed, see [Offline mode](#offline-mode)
- Optional: `tmux` (`sudo apt install tmux`) if you want the terminal session to survive closing your SSH connection
- Optional: `ffmpeg` on the host for converting the audio to WAV: `sudo apt install ffmpeg`


Check that Docker can see the GPU:

```bash
sudo docker run --rm --gpus all ubuntu nvidia-smi
```

**Windows (PowerShell):**

```powershell
docker run --rm --gpus all ubuntu nvidia-smi
```

If this fails, fix the GPU setup before continuing.

## Model storage

All models live in one central directory on the host: `$HOME/models`. Each service uses its own subfolder, because each tool stores models in its own layout and they should not share a folder:

```bash
~/models/
├── ollama/       # Ollama (LLM guide)
├── kokoro/       # Kokoro TTS (Kokoro guide)
└── plapre/       # Plapre-nano-v2 (this guide, created automatically)
```

Create the folder for this service if it does not exist yet:

```bash
mkdir -p ~/models/plapre
```
 
**Windows (PowerShell):**
 
```powershell
New-Item -ItemType Directory -Force -Path "$HOME\models\plapre"
```


The folder is mounted into the container at `/models/plapre`, and the Dockerfile sets `HF_HOME=/models/plapre`, so everything Plapre downloads at runtime ends up in `~/models/plapre` on the server. The files are owned by root, because the container runs as root.

If you want the models directory somewhere else, use that path instead of `$HOME/models` everywhere in this guide, and do the same in the Ollama and Kokoro guides.


## 1. Build the image

From the `Plapre` folder (the one containing the `Dockerfile` and `start.sh`). This needs internet access and takes a while, because the image contains PyTorch and vLLM:

```bash
sudo docker build -t plapre-tts .
```
 
**Windows (PowerShell):**
 
```powershell
docker build -t plapre-tts .
```
 
## 2. Run the container

Run this **once**, in a terminal you can leave open. The container runs in the foreground, so the server log is printed directly in this terminal and the session stays attached to it.

```bash
sudo docker run --gpus all -p 8103:8000 --name plapre-server --restart unless-stopped \
    -v "$HOME/models/plapre:/models/plapre" \
    plapre-tts
```
 
**Windows (PowerShell):**
 
```powershell
docker run --gpus all -p 8103:8000 --name plapre-server --restart unless-stopped `
    -v "${HOME}\models\plapre:/models/plapre" `
    plapre-tts
```
 
What the flags do:

- `--gpus all` gives the container access to the NVIDIA GPU.
- `-p 8103:8000` publishes the API on server port 8103. The second number is the port inside the container and must stay 8000.
- `--restart unless-stopped` starts the container automatically after a reboot, unless you stopped it yourself. After a reboot it starts in the background, so use `docker logs` (see [Viewing the logs](#viewing-the-logs)) to see its output.
- `-v ...:/models/plapre` stores the model in your central models directory.

Because the container is attached to your terminal, **`Ctrl+C` stops the server**, and closing the terminal can stop it too. To keep the session alive when your SSH connection drops, run it inside `tmux`:

```bash
tmux new -s plapre     # start a session, then run the docker run command above
# detach with Ctrl+B, then D (the server keeps running)
tmux attach -t plapre  # reattach later
```

After this, you do not run `docker run` again. Use `docker start -a`, `docker stop` and `docker restart` (see [Managing the container](#managing-the-container)).

## 3. Wait until it is ready

On the first start the container downloads the model and the audio codec, which can take a few minutes. The progress is printed in the terminal where you ran `docker run`.

The server is ready when the log shows that the server has started and `/health` responds (see below). Later starts are faster because everything is read from the cache. Run the commands in the following sections in a **second terminal**.

To confirm the model was stored in the right place:

```bash
du -sh ~/models/plapre
```
 
**Windows (PowerShell):**
 
```powershell
"{0:N2} GB" -f ((Get-ChildItem "$HOME\models\plapre" -Recurse -File | Measure-Object Length -Sum).Sum / 1GB)
```

The size should be larger than a few hundred MB after the first start.

## 4. Health check

```bash
curl http://localhost:8103/health
```
 
**Windows (PowerShell):**
 
```powershell
curl.exe http://localhost:8103/health
```
 
List the available speakers:

```bash
curl http://localhost:8103/v1/speakers
```
 
**Windows (PowerShell):**
 
```powershell
curl.exe http://localhost:8103/v1/speakers
```
 
If an endpoint fails, check the log (see below).

## Limiting GPU memory

When the server starts, vLLM reserves a fixed amount of VRAM and keeps it for as long as the container runs, even when no requests are made. It does not grow and shrink with the actual need. `plapre-serve` only accepts this amount as a fraction of the card's total VRAM (its own default is `0.4`, which would be a different amount on every server).

To reserve the same absolute amount on GPUs of any size, `start.sh` takes a budget in GiB, reads the total VRAM of the card at startup, and converts it to the fraction. The result is printed at the top of the log:

```bash
Plapre GPU budget: 4 GiB -> --gpu-mem 0.084
```

The default budget is `PLAPRE_GPU_GB=4`. This is a starting point that has not been tested on your hardware. To use another budget, set it on the `docker run` command. No rebuild is needed:

```bash
sudo docker rm -f plapre-server
sudo docker run --gpus all -p 8103:8000 --name plapre-server --restart unless-stopped \
    -e PLAPRE_GPU_GB=3 \
    -v "$HOME/models/plapre:/models/plapre" \
    plapre-tts
```
 
**Windows (PowerShell):**
 
```powershell
docker rm -f plapre-server
docker run --gpus all -p 8103:8000 --name plapre-server --restart unless-stopped `
    -e PLAPRE_GPU_GB=3 `
    -v "${HOME}\models\plapre:/models/plapre" `
    plapre-tts
```

If you would rather set a fixed fraction, pass `-e PLAPRE_GPU_MEM=0.1`. It wins over `PLAPRE_GPU_GB`.

The Kanade vocoder (about 400 MiB) is loaded in addition to the budget, so leave some free VRAM for it.

## Check that it uses the GPU

The model runs on the GPU through vLLM. To confirm it, run this on the host while the server is running:

```bash
nvidia-smi
```

A Python process using several GB of VRAM should be listed, and the memory use should be roughly the budget set by `PLAPRE_GPU_GB` (default 4 GiB) plus about 400 MiB for the vocoder. You can also check that the container itself sees the GPU:

```bash
sudo docker exec plapre-server nvidia-smi
```
 
**Windows (PowerShell):**
 
```powershell
docker exec plapre-server nvidia-smi
```
 

## Viewing the logs

The log is printed in the terminal where the container runs. If you need to look at it from another terminal, after the container was started in the background (for example after a reboot), or after the terminal was closed, use `docker logs`:

```bash
sudo docker logs -f plapre-server              # follow the log live (Ctrl+C stops following, not the container)
sudo docker logs --tail 100 plapre-server      # the last 100 lines
sudo docker logs --since 10m plapre-server     # the last 10 minutes
```

**Windows (PowerShell):**
 
```powershell
docker logs -f plapre-server              # follow the log live (Ctrl+C stops following, not the container)
docker logs --tail 100 plapre-server      # the last 100 lines
docker logs --since 10m plapre-server     # the last 10 minutes
```
 
Use `sudo docker ps -a` to see whether the container is running or has exited. If it has exited, `docker logs` still shows why.

## Usage

### Endpoints

| Method | Path               | Description                          |
| ------ | ------------------ | ------------------------------------ |
| GET    | `/health`          | Health check                         |
| GET    | `/v1/speakers`     | Lists the available speakers         |
| POST   | `/v1/audio/speech` | Generates speech and returns raw PCM |

### Request parameters (`POST /v1/audio/speech`)

| Field         | Type   | Example  | Description                    |
| ------------- | ------ | -------- | ------------------------------ |
| `text`        | string | required | The text to speak              |
| `speaker`     | string | `"tor"`  | Speaker name from `/v1/speakers` |
| `temperature` | number | `0.8`    | Sampling temperature           |
| `top_p`       | number | `0.95`   | Nucleus sampling               |
| `top_k`       | number | `50`     | Top-k sampling                 |
| `max_tokens`  | number | `500`    | Maximum number of tokens to generate |

The response is **raw PCM audio** (signed 16-bit little-endian, 24 kHz, mono), not a WAV file. It has to be converted before most players can open it.

### Example: generate a WAV file

Send the request and save the PCM:

```bash
curl -X POST http://localhost:8103/v1/audio/speech \
    -H "Content-Type: application/json" \
    -d '{"text": "Dette er en test. 1 2 3, 3 2 1", "speaker": "tor", "temperature": 0.8, "top_p": 0.95, "top_k": 50, "max_tokens": 500}' \
    --output speech.pcm
```
 
**Windows (PowerShell):**
 
```powershell
$body = @{ text = "Dette er en test. 1 2 3, 3 2 1"; speaker = "tor"; temperature = 0.8; top_p = 0.95; top_k = 50; max_tokens = 500 } | ConvertTo-Json
Invoke-WebRequest -Uri http://localhost:8103/v1/audio/speech -Method Post `
    -ContentType "application/json; charset=utf-8" `
    -Body ([System.Text.Encoding]::UTF8.GetBytes($body)) `
    -OutFile speech.pcm
```
 
Convert it to a WAV file with a timestamp in the name:

```bash
ffmpeg -y -f s16le -ar 24000 -ac 1 -i speech.pcm "speech_$(date +%F_%H-%M-%S).wav"
```
 
**Windows (PowerShell):**
 
```powershell
ffmpeg -y -f s16le -ar 24000 -ac 1 -i speech.pcm "speech_$(Get-Date -Format 'yyyy-MM-dd_HH-mm-ss').wav"
```
 
Or do both in one step without the intermediate file:

```bash
curl -s -X POST http://localhost:8103/v1/audio/speech \
    -H "Content-Type: application/json" \
    -d '{"text": "Dette er en test.", "speaker": "tor"}' \
  | ffmpeg -y -f s16le -ar 24000 -ac 1 -i pipe:0 "speech_$(date +%F_%H-%M-%S).wav"
```

The files are written to the directory you run the commands from.

### Output formats (WAV and base64)

The server only returns **raw PCM**, streamed sentence by sentence. It has no setting for WAV or base64, so the conversion happens after the response is received.

**WAV.** Use `ffmpeg` as shown above. Write to a file, not to a pipe, so the WAV header gets the correct length:

```bash
ffmpeg -y -f s16le -ar 24000 -ac 1 -i speech.pcm speech.wav
```

**Base64.** Encode the PCM or the finished WAV. `-w0` keeps it on one line:

```bash
base64 -w0 speech.pcm > speech.pcm.b64
base64 -w0 speech.wav > speech.wav.b64
```
 
**Windows (PowerShell):**
 
```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("$PWD\speech.pcm")) | Set-Content -NoNewline speech.pcm.b64
[Convert]::ToBase64String([IO.File]::ReadAllBytes("$PWD\speech.wav")) | Set-Content -NoNewline speech.wav.b64
```
 
Or straight from the request, with the PCM as base64:

```bash
curl -s -X POST http://localhost:8103/v1/audio/speech \
    -H "Content-Type: application/json" \
    -d '{"text": "Dette er en test.", "speaker": "tor"}' \
  | base64 -w0 > speech.pcm.b64
```

Base64 of raw PCM can only be played after the receiver adds a WAV header (24 kHz, mono, 16-bit) or decodes it as PCM. If the receiver needs a playable file, send base64 of the WAV.

## Offline mode

Building the image needs internet, but it only installs software (vLLM and the `plapre` package). The model files are **not** part of the image. The running container downloads them on the first start into `~/models/plapre`: Plapre-nano-v2, the Kanade audio codec (`frothywater/kanade-25hz-clean`) and whatever else the codec needs. So the codec is needed at the first run, not at build time, and after that everything is read from the central models directory.

1. Start the container normally (step 3) with internet access and wait until it is ready.
2. Send one real speech request (the `curl` example under [Usage](#usage)), so that everything needed for generating audio is loaded and cached.
3. Check what was cached:

   ```bash
   ls ~/models/plapre/hub
   ```
    
   **Windows (PowerShell):**

   ```powershell
   Get-ChildItem "$HOME\models\plapre\hub"
   ```

   You should see at least `models--syvai--plapre-nano-v2` and `models--frothywater--kanade-25hz-clean`.
4. Stop the container (`Ctrl+C`) and recreate it in offline mode:

   ```bash 
   sudo docker rm -f plapre-server
   sudo docker run --gpus all -p 8103:8000 --name plapre-server --restart unless-stopped \
       -e HF_HUB_OFFLINE=1 -e TRANSFORMERS_OFFLINE=1 \
       -e VLLM_NO_USAGE_STATS=1 -e DO_NOT_TRACK=1 \
       -v "$HOME/models/plapre:/models/plapre" \
       plapre-tts
   ```
 
   **Windows (PowerShell):**
 
   ```powershell
   docker rm -f plapre-server
   docker run --gpus all -p 8103:8000 --name plapre-server --restart unless-stopped `
      -e HF_HUB_OFFLINE=1 -e TRANSFORMERS_OFFLINE=1 `
      -e VLLM_NO_USAGE_STATS=1 -e DO_NOT_TRACK=1 `
      -v "${HOME}\models\plapre:/models/plapre" `
      plapre-tts
   ```
 
   `HF_HUB_OFFLINE` and `TRANSFORMERS_OFFLINE` stop Hugging Face libraries from contacting the Hub. The other two switch off vLLM's usage reporting.
5. Send another speech request. If it works, the model is running offline.

### Proving that it really works without internet (optional)

Run a test container that has no network at all. Because it has no network, the port cannot be published, so the test runs inside the container:

```bash 
sudo docker rm -f plapre-server
sudo docker run --gpus all --network none --name plapre-offline-test \
    -e HF_HUB_OFFLINE=1 -e TRANSFORMERS_OFFLINE=1 \
    -e VLLM_NO_USAGE_STATS=1 -e DO_NOT_TRACK=1 \
    -v "$HOME/models/plapre:/models/plapre" \
    plapre-tts
```
 
**Windows (PowerShell):**
 
```powershell
docker rm -f plapre-server
docker run --gpus all --network none --name plapre-offline-test `
    -e HF_HUB_OFFLINE=1 -e TRANSFORMERS_OFFLINE=1 `
    -e VLLM_NO_USAGE_STATS=1 -e DO_NOT_TRACK=1 `
    -v "${HOME}\models\plapre:/models/plapre" `
    plapre-tts
```
 
When the server has started, run this in a second terminal:

```bash
sudo docker exec plapre-offline-test python -c "import urllib.request as u, json; r = u.Request('http://localhost:8000/v1/audio/speech', json.dumps({'text': 'Dette er en test.', 'speaker': 'tor'}).encode(), {'Content-Type': 'application/json'}); print(len(u.urlopen(r).read()), 'bytes of audio')"
```
 
**Windows (PowerShell):**
 
```powershell
@'
import urllib.request as u, json
r = u.Request("http://localhost:8000/v1/audio/speech", json.dumps({"text": "Dette er en test.", "speaker": "tor"}).encode(), {"Content-Type": "application/json"})
print(len(u.urlopen(r).read()), "bytes of audio")
'@ | Set-Content -Encoding ascii offline_test.py
docker cp offline_test.py plapre-offline-test:/tmp/offline_test.py
docker exec plapre-offline-test python /tmp/offline_test.py
```

If it prints a number of bytes, nothing needs the internet. Stop the test with `Ctrl+C`, remove it with `sudo docker rm plapre-offline-test` and start the real container again with the command from step 4 above. If the test fails, the log shows which file or download is missing. Fix it by starting the container with internet access again (step 3) and repeating the warm-up request.

## Access from another device

Find the IP address of the server:

```bash
hostname -I
```
 
**Windows (PowerShell):**
 
```powershell
ipconfig   # look for "IPv4 Address"
```
 
Then call `http://<server-ip>:8103/v1/audio/speech` from the other device. You may need to open port `8103` in the firewall. Note that ports published by Docker can bypass `ufw` rules, so use the localhost binding from step 3 if the port should not be reachable from the network.

## Managing the container

```bash
sudo docker ps                       # running containers
sudo docker stop plapre-server       # stop
sudo docker start -a plapre-server   # start again, attached to this terminal (log in the foreground)
sudo docker start plapre-server      # start again in the background
sudo docker restart plapre-server    # restart (continues in the background)
sudo docker logs -f plapre-server    # follow the log (Ctrl+C stops following, not the container)
sudo docker rm -f plapre-server      # remove the container (the model stays in ~/models/plapre)
```
 
**Windows (PowerShell):**
 
```powershell
docker ps                       # running containers
docker stop plapre-server       # stop
docker start -a plapre-server   # start again, attached to this terminal (log in the foreground)
docker start plapre-server      # start again in the background
docker restart plapre-server    # restart (continues in the background)
docker logs -f plapre-server    # follow the log (Ctrl+C stops following, not the container)
docker rm -f plapre-server      # remove the container (the model stays in ~/models/plapre)
```
 
`start -a` gives you the same foreground session as step 3: the log is printed in your terminal and `Ctrl+C` stops the server.

### Rebuild after changing the Dockerfile

```bash
sudo docker rm -f plapre-server
sudo docker build -t plapre-tts .
```
 
**Windows (PowerShell):**
 
```powershell
docker rm -f plapre-server
docker build -t plapre-tts .
```
 
Then run the `docker run` command from step 3 again. The model is **not** downloaded again, because it is stored in `~/models/plapre` on the host.

## Configuration

| Setting                 | How                                          | Description                                          |
| ----------------------- | -------------------------------------------- | ---------------------------------------------------- |
| GPU access              | `--gpus all`                                 | Gives the container access to the NVIDIA GPU         |
| Host port               | `-p <host-port>:8000`                        | Port the API is reachable on                         |
| Model directory         | `-v "$HOME/models/plapre:/models/plapre"`    | Where the model cache is stored                      |
| Offline mode            | `-e HF_HUB_OFFLINE=1 -e TRANSFORMERS_OFFLINE=1` | Never contact Hugging Face (cache must be complete) |
| GPU memory budget       | `-e PLAPRE_GPU_GB=4` (Dockerfile default)    | GiB of VRAM vLLM reserves for Plapre, on any GPU size |
| Cache path in container | `HF_HOME` (set in the Dockerfile)            | Do not change unless you also change the mount       |

## Troubleshooting

**GPU not detected.** Check that `--gpus all` is in the `docker run` command, that `nvidia-smi` works on the host, and that the NVIDIA Container Toolkit is installed and Docker was restarted. `sudo docker exec plapre-server nvidia-smi` should list your GPU.

**The download fails or is very slow on the first start.** Check that the server has internet access and look at `sudo docker logs plapre-server`. Anonymous downloads from Hugging Face can be rate limited. If that happens, create a free access token with Read permission, pass it with `-e HF_TOKEN=...` (or `--env-file`), and recreate the container. Nothing else needs to change.

**The model is downloaded again after every rebuild.** The volume is missing or points to the wrong path. Check that the `-v` flag is in the `docker run` command and that `~/models/plapre` contains files after the first start.

**Out of GPU memory.** Ollama, Kokoro and Plapre share the same GPU. Ollama keeps its model loaded permanently (`OLLAMA_KEEP_ALIVE=-1`), Kokoro keeps its model in VRAM while it runs, and vLLM reserves the `PLAPRE_GPU_GB` budget (default 4 GiB) of the GPU for Plapre at startup. If there is not enough free VRAM when Plapre starts, it fails. Check usage with `nvidia-smi`, start Plapre before the other services, or lower the budget with `-e PLAPRE_GPU_GB=3` (see [Limiting GPU memory](#limiting-gpu-memory)).

**Server not responding right after starting.** The model is probably still downloading or loading. Check the log in the terminal where it runs, or use `sudo docker logs -f plapre-server`.

**Port is already in use / port is already allocated.** Another container uses the port. Change the host side of `-p`, for example `-p 8104:8000`.

**"The container name is already in use".** The container already exists. Use `sudo docker start -a plapre-server`, or `sudo docker rm -f plapre-server` first if you want to recreate it.

**Errors in offline mode.** The cache is incomplete. Recreate the container without the offline flags, wait until the server has started, send one speech request, and then follow [Offline mode](#offline-mode) again.

**The server stopped when I closed the terminal.** The container runs in the foreground, so it is tied to the terminal. Run it inside `tmux` (see step 3), or start it again with `sudo docker start -a plapre-server`.

**The WAV file is silent or sounds wrong.** The PCM must be read as signed 16-bit little-endian, 24 kHz, mono (`-f s16le -ar 24000 -ac 1`).
