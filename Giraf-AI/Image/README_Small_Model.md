# 🎨 Stable Diffusion 1.5: Docker API (4GB GPU)

Run **Stable Diffusion 1.5** locally in a Docker container and generate images through a simple REST API. It runs in half precision (fp16), so it fits on a GPU with **4GB of VRAM**.

---

## 📋 Prerequisites

- [x] Windows or Linux
- [x] [Docker](https://docs.docker.com/get-docker/) installed (with GPU support)
- [x] An NVIDIA GPU with **4GB+ VRAM**
- [x] A [Hugging Face](https://huggingface.co/) account (optional: the model is public, but logging in avoids rate limits)

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

### 3. Log in to Hugging Face (optional)

```bash
hf auth login
```

### 4. Download the model

```bash
python -c "from huggingface_hub import snapshot_download; snapshot_download(repo_id='stable-diffusion-v1-5/stable-diffusion-v1-5', local_dir='stable-diffusion-v1-5', local_dir_use_symlinks=False, allow_patterns=['model_index.json', '*/*.json', '*/*.txt', '*.fp16.safetensors'])"
```

> [!NOTE]
> This downloads the model into a new folder called `stable-diffusion-v1-5` inside your current folder.

---

## 🐳 Run the container

Open a terminal and `cd` into the folder containing `app.py` and the `Dockerfile`.

> [!IMPORTANT]
> Your `app.py` must load the model with low-VRAM settings, otherwise it can run out of memory on 3GB:
>
> ```python
> pipe = StableDiffusionPipeline.from_pretrained(
>     "/models/stable-diffusion-v1-5",
>     torch_dtype=torch.float16,
>     variant="fp16",
> ).to("cuda")
> pipe.enable_attention_slicing()
> pipe.enable_vae_slicing()
> ```

### 1. Build the image

```bash
docker build -t sd15-api .
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

**Example with `stable-diffusion-v1-5`:**

<details open>
<summary><b>🪟 Windows (PowerShell)</b></summary>

```powershell
docker run --gpus all -p 8000:8000 `
    -v "C:\Users\ulrik\stable-diffusion-v1-5:/models/stable-diffusion-v1-5" `
    sd15-api
```

</details>

<details open>
<summary><b>🐧 Linux</b></summary>

```bash
docker run --gpus all -p 8000:8000 \
    -v "$HOME/stable-diffusion-v1-5:/models/stable-diffusion-v1-5" \
    sd15-api
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
    size                = "512x512"
    n                   = 1
    num_inference_steps = 20
    guidance_scale      = 7.5
    seed                = -1
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:8000/v1/images/generations" `
    -Method Post -ContentType "application/json" -Body $body -TimeoutSec 300

$bytes = [Convert]::FromBase64String($response.data[0].b64_json)
[IO.File]::WriteAllBytes("$PWD\generated.png", $bytes)
Write-Host "Saved: $PWD\generated.png"
```

**Linux / macOS (curl + jq):**

```bash
curl -s http://localhost:8000/v1/images/generations \
  -H "Content-Type: application/json" \
  -d '{"prompt":"apple","negative_prompt":"","size":"512x512","n":1,"num_inference_steps":20,"guidance_scale":7.5,"seed":-1}' \
  | jq -r '.data[0].b64_json' | base64 -d > generated.png
```

The image is saved as `generated.png` in your current folder. 🎉

### Request parameters

| Parameter             | Example   | Description                                  |
| --------------------- | --------- | -------------------------------------------- |
| `prompt`              | `"apple"` | What you want to generate                    |
| `negative_prompt`     | `""`      | What the image should avoid                  |
| `size`                | `512x512` | Output resolution                            |
| `n`                   | `1`       | Number of images to generate                 |
| `num_inference_steps` | `20`      | Denoising steps (more = slower, more detail) |
| `guidance_scale`      | `7.5`     | How closely to follow the prompt             |
| `seed`                | `-1`      | Random seed (`-1` = random each time)        |

> [!TIP]
> On 3GB, stick to `512x512` (or up to about `512x768`) and keep `n` at `1`. Larger sizes or batches can cause out-of-memory errors.
