# Ollama in Docker: Setup Guide (Windows and Linux)

This guide explains how to:

1. Run **Ollama** in Docker with your models stored in a folder you choose
2. Download an LLM and keep it loaded in VRAM
3. Send HTTP requests to the LLM and get only the generated text

Every command is given for both Linux (Ubuntu) and Windows (PowerShell).

## Conventions used in this guide

- **Linux** means Ubuntu with bash. A backslash `\` continues a line. `sudo` is needed unless your user is in the `docker` group.
- **Windows** means PowerShell with Docker Desktop. A backtick continues a line. There is no `sudo`; if a command is refused, open PowerShell as Administrator.
- Use `localhost` on the machine itself. From another machine, replace it with the server's address (`IP_HERE`).
- Port: Ollama is published on **8101**.

## Prerequisites

**Linux**

- Docker Engine, an NVIDIA GPU with its driver, and the NVIDIA Container Toolkit (needed for `--gpus`).
- `jq` for the curl examples: `sudo apt install jq`

**Windows**

- Windows 10/11 with [Docker Desktop](https://www.docker.com/products/docker-desktop/) (WSL2 backend) and an NVIDIA GPU with its driver.

**Check that Docker can see the GPU**

Linux:

```bash
sudo docker run --rm --gpus all ubuntu nvidia-smi
```

Windows:

```powershell
docker run --rm --gpus all ubuntu nvidia-smi
```

If this fails, fix the GPU setup before continuing.

---

## 1. Create a folder for Ollama's models

Ollama's models are stored in a normal folder on your machine, so they stay in your `models` directory next to your other AI models and survive when the container is removed.

Linux:

```bash
mkdir -p ~/models/ollama
```

Windows:

```powershell
New-Item -ItemType Directory -Force "$HOME\models\ollama"
```

Keep this folder separate from other model folders. Ollama stores models in its own format and should not share a folder with files that have another layout. On Linux the files will be owned by root, because the container runs as root.

If you would rather let Docker manage the storage, create a named volume (`docker volume create ollama`) and use `-v ollama:/root/.ollama` in step 2 instead of the folder mount.

---

## 2. Start Ollama

Run this **once**. It creates the container.

Linux:

```bash
sudo docker run -d \
  --gpus=all \
  --restart unless-stopped \
  -e OLLAMA_KEEP_ALIVE=-1 \
  -v "$HOME/models/ollama:/root/.ollama" \
  -p 8101:11434 \
  --name ollama \
  ollama/ollama
```

Windows:

```powershell
docker run -d `
  --gpus=all `
  --restart unless-stopped `
  -e OLLAMA_KEEP_ALIVE=-1 `
  -v "${HOME}\models\ollama:/root/.ollama" `
  -p 8101:11434 `
  --name ollama `
  ollama/ollama
```

What the flags do:

- `-d` runs the container in the background.
- `--gpus=all` gives the container access to the NVIDIA GPU.
- `--restart unless-stopped` starts the container automatically after a reboot, unless you stopped it yourself.
- `-e OLLAMA_KEEP_ALIVE=-1` keeps models loaded in memory forever instead of unloading them after 5 minutes.
- `-v ...:/root/.ollama` stores Ollama's models in your folder, so they survive when the container is removed.
- `-p 8101:11434` publishes Ollama on server port 8101. The first number is the outside port, the second is the port inside the container and must stay 11434.
- `--name ollama` names the container.

If only the machine itself needs access (for example a C# app on the same server), bind to localhost with `-p 127.0.0.1:8101:11434`. Ollama has no authentication, so anyone who can reach the port can use your GPU.

After this, you do not run `docker run` again. Use `docker start`, `docker stop` and `docker restart` (see the command list near the end).

---

## 3. Check that Ollama is running

Linux:

```bash
sudo docker ps
curl http://localhost:8101/api/tags
```

Windows:

```powershell
docker ps
Invoke-RestMethod http://localhost:8101/api/tags
```

You should see a container named `ollama` with status `Up` and ports like `0.0.0.0:8101->11434/tcp`.

---

## 4. Download an LLM

The same command works on both systems (add `sudo` on Linux):

```bash
sudo docker exec -it ollama ollama pull qwen3.5:9b
```

```powershell
docker exec -it ollama ollama pull qwen3.5:9b
```

`docker exec` talks to the container directly, so the published port does not matter here.

---

## 5. Check installed models and storage

Linux:

```bash
sudo docker exec -it ollama ollama list
du -sh ~/models/ollama
```

Windows:

```powershell
docker exec -it ollama ollama list
"{0:N1} GB" -f ((Get-ChildItem "$HOME\models\ollama" -Recurse | Measure-Object Length -Sum).Sum / 1GB)
```

The size check confirms the models are being written to your `models` folder.

---

## 6. Load the model and check VRAM

A model only loads into VRAM on its first request. To load it right away, send a request with no prompt.

Linux:

```bash
curl -s http://localhost:8101/api/generate -d '{"model": "qwen3.5:9b"}'
```

Windows:

```powershell
Invoke-RestMethod -Uri "http://localhost:8101/api/generate" -Method Post -ContentType "application/json" -Body '{"model":"qwen3.5:9b"}'
```

Then check where the model is running.

Linux:

```bash
sudo docker exec -it ollama ollama ps
nvidia-smi
watch -n 1 nvidia-smi
```

Windows:

```powershell
docker exec -it ollama ollama ps
nvidia-smi
nvidia-smi -l 1
```

How to read `ollama ps`:

- `100% GPU` means the model is fully in VRAM. This is what you want.
- A split such as `48%/52% CPU/GPU` means it did not fit, so part runs on the CPU and is much slower.
- `100% CPU` means the GPU is not being used.
- The `UNTIL` column shows `Forever` when `OLLAMA_KEEP_ALIVE=-1` is active.

A 9B model at the default 4-bit quantization needs roughly 6-7 GB plus room for the context window.

---

## 7. Send an HTTP request

Ollama's `/api/generate` endpoint is built into Ollama itself. You do not create it. You only call it.

**Linux (curl)**

```bash
curl -s http://localhost:8101/api/generate -d '{
  "model": "qwen3.5:9b",
  "think": false,
  "prompt": "Where do giraffes live? Answer in two sentences.",
  "stream": false,
  "options": { "temperature": 0, "num_predict": 600 }
}' | jq -r '.response'
```

**Windows (PowerShell)**

```powershell
$body = @{
    model = "qwen3.5:9b"
    think = $false
    prompt = "Where do giraffes live? Answer in two sentences."
    stream = $false
    options = @{
        temperature = 0
        num_predict = 50
    }
} | ConvertTo-Json -Depth 10

$response = Invoke-RestMethod `
    -Uri "http://localhost:8101/api/generate" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body

$response.response
```

Note: in PowerShell, `curl` is an alias for `Invoke-WebRequest`, so use `Invoke-RestMethod` as shown (or `curl.exe` for the real curl).

**JetBrains Rider (`ollama.http`)**

```http
POST http://localhost:8101/api/generate
Content-Type: application/json

{
  "model": "qwen3.5:9b",
  "think": false,
  "prompt": "Where do giraffes live?",
  "stream": false,
  "options": {
    "temperature": 0,
    "num_predict": 50
  }
}

> {%
    console.log(response.body.response);
%}
```

Use the model name exactly as shown by `ollama list`.

**Replies that stop mid-sentence**

`num_predict` caps the length of the reply in tokens. If the cap is reached, the text is cut off, even in the middle of a sentence. Raise it, remove it, or ask for a short answer in the prompt. To see whether a reply was cut off, look at `done_reason`: `length` means the cap was hit and `stop` means the model finished.

```bash
curl -s http://localhost:8101/api/generate -d '{"model":"qwen3.5:9b","think":false,"prompt":"Where do giraffes live?","stream":false,"options":{"num_predict":100}}' | jq '{done_reason, response}'
```

---

## 8. Use a different model

```bash
sudo docker exec -it ollama ollama pull llama3.2:1b
sudo docker exec -it ollama ollama pull llama3.1:8b
```

On Windows use the same commands without `sudo`. Then change the `"model"` field in your request to the new name. Do not send a short `keep_alive` value in requests, because it overrides the permanent setting for that model.

---

## 9. Changing the port or settings later

Published ports, environment variables and mounts are fixed when a container is created, so you recreate the container. Your models are safe, because they live in the folder and not in the container.

Linux:

```bash
sudo docker rm -f ollama
# then run the step 2 command again with the new values
```

Windows:

```powershell
docker rm -f ollama
# then run the step 2 command again with the new values
```

Port notes:

- With `-p 8101:11434`, port 11434 on the server is **not** open. Use `localhost:8101` from the server and `IP_HERE:8101` from other machines.
- Inside the container Ollama still listens on 11434. Commands run with `docker exec` and other containers on the same Docker network (using the container name, `http://ollama:11434`) use that internal port.
- A host port can only be used by one process. If port 8101 is taken, choose another outside port.
- Open the port in the firewall if other machines need to reach it.

---

## 10. Useful Docker commands

Linux (on Windows use the same commands without `sudo`):

```bash
sudo docker ps                  # running containers
sudo docker ps -a               # all containers, including stopped ones
sudo docker start ollama        # start again
sudo docker stop ollama         # stop
sudo docker restart ollama      # restart
sudo docker logs ollama         # view logs
sudo docker logs -f ollama      # follow logs (Ctrl+C to stop)
sudo docker port ollama         # show published ports
sudo docker rm -f ollama        # remove the container (models stay in the folder)
```

---

## 11. Troubleshooting

- **Reply stops mid-sentence:** the `num_predict` limit was reached. See step 7.
- **Connection refused on port 11434:** only the published port is open. Use 8101.
- **"The container name is already in use":** the container already exists. Use `docker start ollama`, or `docker rm -f ollama` first if you want to recreate it.
- **"Port is already allocated":** another process or container uses that host port. Choose a different outside port.
- **Model runs on the CPU:** check `ollama ps`, run the GPU test from the prerequisites, and read `docker logs ollama` for GPU detection messages.
- **First request is slow:** the model is loading into VRAM. Warm it up as shown in step 6.
- **Out of memory:** check `nvidia-smi`. Use a smaller or more heavily quantized model.
- **Calling from another container:** `localhost` there points to that container. Use the Docker network name (`http://ollama:11434`) or `host.docker.internal:8101`.