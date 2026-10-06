# 🎨 Stable Diffusion XL Base 1.0: Docker API

Run **Stable Diffusion XL Base 1.0** locally in a Docker container and generate images through a simple REST API. It runs in half precision (fp16) with CPU offloading, so it works on GPUs with **6GB+ of VRAM** (8GB+ recommended for speed).

---

## 📋 Prerequisites

- [x] Windows or Linux
- [x] [Docker](https://docs.docker.com/get-docker/) installed (with GPU support)
- [x] A [Hugging Face](https://huggingface.co/) account that has **accepted the model's terms** on the [model page](https://huggingface.co/stabilityai/stable-diffusion-xl-base-1.0)

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
python -m pip install -U huggingface_hub
```

</details>

<details open>
<summary><b>🐧 Linux</b></summary>

```bash
sudo apt update
sudo apt install -y python3 python3-pip python3-venv
python3 -m pip install -U huggingface_hub
```

</details>

### 3. Log in to Hugging Face

```bash
hf auth login
```

### 4. Download the model

```bash
python -c "from huggingface_hub import snapshot_download; snapshot_download(repo_id='stabilityai/stable-diffusion-xl-base-1.0', local_dir='stable-diffusion-xl-base-1.0', local_dir_use_symlinks=False)"```
```

> [!NOTE]
> This downloads the model into a new folder called `stable-diffusion-xl-base-1.0` inside your current folder.

---

## 🐳 Run the container

Open a terminal and `cd` into the folder containing `app.py` and the `Dockerfile`.

### 1. Build the image

```bash
docker build -t sdxl-api .
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
docker run --gpus all -p PORT:PORT \
    -v "/path/to/model/folder:/models/MODEL_NAME_HERE" \
    name-of-image
```

</details>

**Example with `stable-diffusion-xl-base-1.0`:**

<details open>
<summary><b>🪟 Windows (PowerShell)</b></summary>

```powershell
docker run --gpus all -p 8000:8000 `
    -v "C:\Users\ulrik\stable-diffusion-xl-base-1.0:/models/stable-diffusion-xl-base-1.0" `
    sdxl-api
```

</details>

<details open>
<summary><b>🐧 Linux</b></summary>

```bash
docker run --gpus all -p 8000:8000 \
    -v "$HOME/stable-diffusion-xl-base-1.0:/models/stable-diffusion-xl-base-1.0" \
    sdxl-api
```

</details>

Wait until the logs say:

```text
Model ready.
```

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
    num_inference_steps = 30
    guidance_scale      = 7.0
    seed                = -1
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:8000/v1/images/generations" `
    -Method Post -ContentType "application/json" -Body $body -TimeoutSec 600

$bytes = [Convert]::FromBase64String($response.data[0].b64_json)
[IO.File]::WriteAllBytes("$PWD\generated.jpg", $bytes)
Write-Host "Saved: $PWD\generated.jpg"
```

**Linux / macOS (curl + jq):**

```bash
curl -s http://localhost:8000/v1/images/generations \
  -H "Content-Type: application/json" \
  -d '{"prompt":"apple","negative_prompt":"","size":"1024x1024","n":1,"num_inference_steps":30,"guidance_scale":7.0,"seed":-1}' \
  | jq -r '.data[0].b64_json' | base64 -d > generated.jpg
```

The image is saved as `generated.jpg` in your current folder. 🎉

### Request parameters

| Parameter             | Example     | Description                                  |
| --------------------- | ----------- | -------------------------------------------- |
| `prompt`              | `"apple"`   | What you want to generate                    |
| `negative_prompt`     | `""`        | What the image should avoid                  |
| `size`                | `1024x1024` | Output resolution                            |
| `n`                   | `1`         | Number of images to generate                 |
| `num_inference_steps` | `30`        | Denoising steps (more = slower, more detail) |
| `guidance_scale`      | `7.0`       | How closely to follow the prompt             |
| `seed`                | `-1`        | Random seed (`-1` = random each time)        |

> [!TIP]
> SDXL is trained for around 1024x1024 (or similar total pixel counts such as `896x1152` or `1152x896`). Lower resolutions like 512x512 give noticeably worse results. If you run out of VRAM, keep `n` at `1`, or in `app.py` replace `enable_model_cpu_offload()` with `enable_sequential_cpu_offload()`, which uses much less VRAM but is considerably slower.
