# 🎨 Qwen-Image-2.1-Turbo: Docker API

Run **Qwen-Image-2.1-Turbo** locally in a Docker container and generate images through a simple REST API.

---

## 📋 Prerequisites

- [x] Windows or Linux
- [x] [Docker](https://docs.docker.com/get-docker/) installed (with GPU support)
- [x] A [Hugging Face](https://huggingface.co/) account with access to the [Qwen-Image-2.1-Turbo model](https://huggingface.co/Qwen/Qwen-Image-2.1-Turbo)

---

## 🚀 Setup

### 1. Create a working folder

Create a folder anywhere on your PC and open a terminal in that directory.

### 2. Install Python and the Hugging Face CLI

Skip this step if both are already installed.

<details open>
<summary><b>🪟 Windows</b></summary>

```powershell
winget install Python.Python.3.12
py -3 -m pip install -U huggingface_hub
```

</details>

<details open>
<summary><b>🐧 Linux</b></summary>

```bash
sudo apt update
sudo apt install -y python3 python3-pip python3-venv
python3 -m pip install -U huggingface_hub
```

> [!NOTE]
> You may have to restart the terminal to use python commands.


</details>

### 3. Log in to Hugging Face

```bash
hf auth login
```

### 4. Download the model

**Windows (PowerShell):**

```powershell
py -3 -c "from huggingface_hub import snapshot_download; snapshot_download(repo_id='Qwen/Qwen-Image-2.1-Turbo', local_dir='qwen-image-2.1-turbo')"
if (-not (Test-Path ".\qwen-image-2.1-turbo\model_index.json")) {
    throw "Model download is incomplete or the current directory is incorrect."
}
```

**Linux / macOS:**

```bash
python3 -c "from huggingface_hub import snapshot_download; snapshot_download(repo_id='Qwen/Qwen-Image-2.1-Turbo', local_dir='qwen-image-2.1-turbo')"
test -f qwen-image-2.1-turbo/model_index.json || { echo "Model download is incomplete or the current directory is incorrect." >&2; exit 1; }
```

> [!NOTE]
> This downloads the model into a new folder called `qwen-image-2.1-turbo` inside your current folder.
> Before starting Docker, verify that this folder contains `model_index.json`.

---

## 🐳 Run the container

Open a terminal and `cd` into the folder containing `app.py` and the `Dockerfile`.

### 1. Build the image

```bash
docker build -t qwen-image-21-turbo-api .
```

### 2. Start the container

**Template:**

<details open>
<summary><b>🪟 Windows (PowerShell)</b></summary>

```powershell
docker run --gpus all -p PORT:PORT `
    -v "C:\PATH\TO\MODEL\FOLDER:/models/MODEL_NAME_HERE" `
    name-of-image
```

</details>

<details open>
<summary><b>🐧 Linux</b></summary>

```bash
sudo docker run --gpus all -p PORT:PORT \
    -v "/path/to/model/folder:/models/MODEL_NAME_HERE" \
    name-of-image
```

</details>

**Example with `qwen-image-2.1-turbo`:**

<details open>
<summary><b>🪟 Windows (PowerShell)</b></summary>

```powershell
docker run --gpus all -p 8000:8000 `
    -v "C:\Users\ulrik\qwen-image-2.1-turbo:/models/qwen-image-2.1-turbo" `
    qwen-image-21-turbo-api
```

</details>

<details open>
<summary><b>🐧 Linux</b></summary>

```bash
sudo docker run --gpus all -p 8000:8000 \
    -v "$HOME/qwen-image-2.1-turbo:/models/qwen-image-2.1-turbo" \
    qwen-image-21-turbo-api
```

</details>

Wait until the logs say:

```text
Model ready.
```

The container enables sequential CPU offload by default. This keeps the
pipeline's peak GPU allocation suitable for GPUs with less than 20 GB of VRAM,
at the cost of slower generation. The API also generates `n` images
sequentially instead of creating a batch, preventing the request size from
multiplying the peak VRAM requirement. VAE tiling/slicing, attention slicing,
and the CUDA allocator's expandable segments are enabled automatically.
Qwen-Image-2.1-Turbo uses its built-in 8-step sampling schedule by default.
Requests are limited to 1024x1024 per image by default; increase `MAX_IMAGE_SIZE`
only if the available VRAM has been tested with the larger resolution.

For a GPU with more available VRAM, `CPU_OFFLOAD_MODE=model` is a faster
alternative. Do not use `CPU_OFFLOAD_MODE=none` unless the complete pipeline
fits in VRAM.

`CPU_OFFLOAD_MODE=group` is an intermediate option. It offloads groups of
transformer blocks instead of individual layers, reducing transfer overhead
while using less VRAM than model-level offload. `GROUP_BLOCKS_PER_GROUP`
controls the trade-off; start with `2` and monitor `nvidia-smi`. The setting
does not provide a hard 12 GB limit, because peak usage also depends on the
pipeline and image dimensions.

If a request reports a CUDA allocator assertion, restart the container before
trying again; a failed CUDA allocation can leave the process unusable. The
group-offload path does not call `torch.cuda.empty_cache()` between images
because that can invalidate active offload allocations.

```powershell
docker run --gpus all -p 8000:8000 `
    -e CPU_OFFLOAD_MODE=group `
    -e GROUP_BLOCKS_PER_GROUP=2 `
    -v "C:\Users\ulrik\qwen-image-2.1-turbo:/models/qwen-image-2.1-turbo" `
    qwen-image-21-turbo-api
```

For an RTX 5080 (16 GB VRAM), keep the default sequential mode. If you
previously started the container with `CPU_OFFLOAD_MODE=model`, stop and
remove that container before starting it again:

```powershell
docker ps -a --filter "ancestor=qwen-image-21-turbo-api"
docker stop <container-id>
docker rm <container-id>
docker run --gpus all -p 8000:8000 `
    -e CPU_OFFLOAD_MODE=sequential `
    -v "C:\Users\ulrik\qwen-image-2.1-turbo:/models/qwen-image-2.1-turbo" `
    qwen-image-21-turbo-api
```

Close other GPU applications before generating. A 512x512 request still
requires the model weights and denoising workspace in addition to the output
resolution, so reducing the size alone does not guarantee that model-level
offload will fit.

---

## 🩺 Check the container health

Before generating images, confirm the container is up, see which model is loaded, and verify the GPU is enabled.

**PowerShell:**

```powershell
Invoke-RestMethod -Uri "http://localhost:8000/health"
```

**Linux / macOS:**

```bash
curl -s http://localhost:8000/health
```

Or just open this in your browser: <http://localhost:8000/health>

> [!TIP]
> If the connection is refused, the model may still be loading. Check the container logs and wait for `Model ready.`
>
> ```bash
> docker logs -f <container-name-or-id>
> ```

---

## 🖼️ Generate an image

Once the model is ready, send a test request.

**PowerShell:**

```powershell
$body = @{
    prompt              = "apple"
    negative_prompt     = ""
    size                = "1024x1024"
    n                   = 1
    num_inference_steps = 8
    guidance_scale      = 1.0
    seed                = -1
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:8000/v1/images/generations" `
    -Method Post -ContentType "application/json" -Body $body -TimeoutSec 300

$bytes = [Convert]::FromBase64String($response.data[0].b64_json)
[IO.File]::WriteAllBytes("$PWD\generated.jpg", $bytes)
Write-Host "Saved: $PWD\generated.jpg"
```

**Linux / macOS (curl + jq):**

```bash
curl -s http://localhost:8000/v1/images/generations \
  -H "Content-Type: application/json" \
  -d '{"prompt":"apple","negative_prompt":"","size":"512x512","n":1,"num_inference_steps":8,"guidance_scale":1.0,"seed":-1}' \
  | jq -r '.data[0].b64_json' | base64 -d > generated.jpg
```

The image is saved as `generated.jpg` in your current folder. 🎉

### Request parameters

| Parameter             | Example   | Description                                  |
| --------------------- | --------- | -------------------------------------------- |
| `prompt`              | `"apple"` | What you want to generate                    |
| `negative_prompt`     | `""`      | What the image should avoid                  |
| `size`                | `512x512` | Output resolution                            |
| `n`                   | `1`       | Number of images to generate                 |
| `num_inference_steps` | `8`       | Denoising steps (the Turbo checkpoint is designed for 8 steps) |
| `guidance_scale`      | `1.0`     | Classifier-free guidance strength (`true_cfg_scale` in Qwen's pipeline) |
| `seed`                | `-1`      | Random seed (`-1` = random each time)        |
