# 🎨 Qwen-Image-2.1-Turbo: Docker API

Run **Qwen-Image-2.1-Turbo** locally in a Docker container and generate images through a simple REST API.

* * *

## 📋 Prerequisites

- Windows or Linux
- [Docker](https://docs.docker.com/get-docker/) installed (with GPU support)
- An NVIDIA GPU with about **24 GB of VRAM** (tested on an NVIDIA A10)
- About **40 GB of free RAM** and about **35 GB of free disk space**
- A [Hugging Face](https://huggingface.co/) account with access to the [Qwen-Image-2.1-Turbo model](https://huggingface.co/Qwen/Qwen-Image-2.1-Turbo)
- The files `app.py`, `requirements.txt` and `Dockerfile` from this folder of the repository (download or clone the repository)

> [!NOTE]
> Less RAM? See [Optional settings](#️-optional-settings) below.

* * *

## 🚀 Setup

### 1. Create a working folder

Create a folder anywhere on your PC and open a terminal in that directory. This is where the model will be downloaded.

### 2. Install Python and the Hugging Face CLI

Skip this step if both are already installed.

**🪟 Windows**

```
winget install Python.Python.3.12
py -3 -m pip install -U huggingface_hub
```

**🐧 Linux**

```
sudo apt update
sudo apt install -y python3 python3-pip python3-venv
python3 -m pip install -U huggingface_hub
```

> [!NOTE]
> You may have to restart the terminal to use python commands.

### 3. Log in to Hugging Face

```
hf auth login
```

You will be asked for an access token. Create one at [huggingface.co/settings/tokens](https://huggingface.co/settings/tokens) (a read token is enough) and paste it into the terminal.

### 4. Download the model

This downloads about 32 GB, so it can take a while.

**Windows (PowerShell):**

```
py -3 -c "from huggingface_hub import snapshot_download; snapshot_download(repo_id='Qwen/Qwen-Image-2.1-Turbo', local_dir='qwen-image-2.1-turbo')"
if (-not (Test-Path ".\qwen-image-2.1-turbo\model_index.json")) {
    throw "Model download is incomplete or the current directory is incorrect."
}
```

**Linux / macOS:**

```
python3 -c "from huggingface_hub import snapshot_download; snapshot_download(repo_id='Qwen/Qwen-Image-2.1-Turbo', local_dir='qwen-image-2.1-turbo')"
test -f qwen-image-2.1-turbo/model_index.json || { echo "Model download is incomplete or the current directory is incorrect." >&2; exit 1; }
```

> [!NOTE]
> This downloads the model into a new folder called `qwen-image-2.1-turbo` inside your current folder. Before starting Docker, verify that this folder contains `model_index.json`.

* * *

## 🐳 Run the container

Open a terminal and `cd` into the folder containing `app.py` and the `Dockerfile`.

### 1. Build the image

```
docker build -t qwen-image-21-turbo-api .
```

This builds a Docker image (a ready-to-run package with the app and its libraries). The first build takes a few minutes. The model itself is not part of the image; you connect it in the next step.

> [!IMPORTANT]
> If you change the `Dockerfile` or `app.py`, run the build command again before starting the container.

### 2. Start the container

**Template:**

**🪟 Windows (PowerShell)**

```
docker run --gpus all -p PORT:PORT `
    -v "C:\PATH\TO\MODEL\FOLDER:/models/MODEL_NAME_HERE" `
    name-of-image
```

**🐧 Linux**

```
sudo docker run --gpus all -p PORT:PORT \
    -v "/path/to/model/folder:/models/MODEL_NAME_HERE" \
    name-of-image
```

**Example with `qwen-image-2.1-turbo`:**

**🪟 Windows (PowerShell)**

```
docker run --gpus all -p 8000:8000 `
    -v "C:\Users\<you>\qwen-image-2.1-turbo:/models/qwen-image-2.1-turbo" `
    qwen-image-21-turbo-api
```

**🐧 Linux**

```
sudo docker run --gpus all -p 8000:8000 \
    -v "$HOME/qwen-image-2.1-turbo:/models/qwen-image-2.1-turbo" \
    qwen-image-21-turbo-api
```

What the options mean:

- `--gpus all`: lets the container use your GPU.
- `-p 8000:8000`: makes the API available at `http://localhost:8000`.
- `-v "folder on your PC:folder in the container"`: shares the model folder with the container. The folder on your PC (before the colon) must exist and contain `model_index.json`. The path inside the container (after the colon) must be exactly `/models/qwen-image-2.1-turbo`, in lowercase.

Wait until the logs say:

```
Model ready.
```

Loading takes a minute or two. The first image after a start is slower than the following ones.

The container splits the model between the GPU and your RAM: the part that generates the image stays on the GPU, while the text encoder and the VAE (the parts that read your prompt and produce the final picture) stay in RAM. This keeps generation fast and leaves part of the GPU memory free for other programs. These settings are already in the `Dockerfile`, so you do not need to add anything to the start command. A 1024x1024 image takes about 15 seconds on an NVIDIA A10.

The API generates `n` images one after the other, and requests are limited to 1024x1024 per image by default.

### ⚙️ Optional settings

You only need these if the default does not fit your machine. Add the option to the start command, before the image name, for example for a machine with less RAM:

**🪟 Windows (PowerShell)**

```
docker run --gpus all -p 8000:8000 `
    -e ENCODER_DTYPE=bfloat16 `
    -v "C:\Users\<you>\qwen-image-2.1-turbo:/models/qwen-image-2.1-turbo" `
    qwen-image-21-turbo-api
```

**🐧 Linux**

```
sudo docker run --gpus all -p 8000:8000 \
    -e ENCODER_DTYPE=bfloat16 \
    -v "$HOME/qwen-image-2.1-turbo:/models/qwen-image-2.1-turbo" \
    qwen-image-21-turbo-api
```

| Setting | Default | Description |
|---|---|---|
| `ENCODER_DTYPE` | `float32` | Number format of the text encoder. `float32` is faster on most CPUs but needs about 34 GB of RAM. `bfloat16` needs about 17 GB, but the text step is slower on CPUs without `bfloat16` support. |
| `VAE_TILING` | `0` | Set to `1` to decode the image in tiles to save memory. This can leave visible lines or seams in the image. |
| `MAX_IMAGE_SIZE` | `1024` | Largest allowed width or height. Increase it only if you have tested that your GPU memory is enough. |

* * *

## 🩺 Check the container health

Before generating images, confirm the container is up, see which model is loaded, and verify where each part of the model is.

**PowerShell:**

```
Invoke-RestMethod -Uri "http://localhost:8000/health"
```

**Linux / macOS:**

```
curl -s http://localhost:8000/health
```

Or just open this in your browser: [http://localhost:8000/health](http://localhost:8000/health)

The response should show `"offload_mode": "split"` and the transformer on `cuda:0` (the GPU).

> [!TIP]
> If the connection is refused, the model may still be loading. Check the container logs and wait for `Model ready.`
>
> ```
> docker logs -f <container-name-or-id>
> ```

* * *

## 🖼️ Generate an image

Once the model is ready, send a test request from a **second terminal** (the first one shows the container logs).

**PowerShell:**

```
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
[IO.File]::WriteAllBytes("$PWD\generated.png", $bytes)
Write-Host "Saved: $PWD\generated.png"
```

**Linux / macOS (curl + jq):**

Install `jq` first if needed: `sudo apt install -y jq`

```
curl -s http://localhost:8000/v1/images/generations \
  -H "Content-Type: application/json" \
  -d '{"prompt":"apple","negative_prompt":"","size":"1024x1024","n":1,"num_inference_steps":8,"guidance_scale":1.0,"seed":-1}' \
  | jq -r '.data[0].b64_json' | base64 -d > generated.png
```

The image is saved as `generated.png` in your current folder. 🎉

### Request parameters

| Parameter             | Example   | Description                                                                                             |
|-----------------------|-----------|---------------------------------------------------------------------------------------------------------|
| `prompt`              | `"apple"` | What you want to generate                                                                               |
| `negative_prompt`     | `""`      | What the image should avoid. Only has an effect when `guidance_scale` is above 1.0                      |
| `size`                | `1024x1024` | Output resolution                                                                                     |
| `n`                   | `1`       | Number of images to generate                                                                            |
| `num_inference_steps` | `8`       | Denoising steps (the Turbo checkpoint is designed for 8 steps)                                          |
| `guidance_scale`      | `1.0`     | Classifier-free guidance strength (`true_cfg_scale` in Qwen's pipeline). `1.0` is fastest and recommended |
| `seed`                | `-1`      | Random seed (`-1` = random each time)                                                                   |

> [!TIP]
> Put everything you want to see (and avoid) in the `prompt` itself, for example "flat vector pictogram, plain white background, no shadows". At the default `guidance_scale` of `1.0` the `negative_prompt` is ignored.

* * *

## 🛠️ Troubleshooting

| Problem | What to do |
|---|---|
| `neither a valid local path nor a valid repo id` | The path after the colon in `-v` must be exactly `/models/qwen-image-2.1-turbo` (lowercase). |
| `no file named model_index.json found` | The folder before the colon in `-v` is wrong or empty. Check that `model_index.json` is inside it. On Linux with `sudo`, use the full path (for example `/home/<you>/qwen-image-2.1-turbo`) if `$HOME` points to the wrong folder. |
| `CPU_OFFLOAD_MODE must be split or model, got '...'` | The image is outdated. Run the build command again and start the container again. |
| The request returns an error or the saved image is tiny | Run the request without the last `jq ... \| base64 -d` part to read the error message, and check `docker logs`. |
| Connection refused | The model is still loading. Wait for `Model ready.` in the logs. |
| Out of memory (GPU) or the container stops while loading (RAM) | Close other programs that use the GPU. If RAM runs out, use `-e ENCODER_DTYPE=bfloat16` (see Optional settings). On Windows, Docker may be limited by WSL2; raise `memory=` in `%UserProfile%\.wslconfig` and run `wsl --shutdown`. |
| Lines or seams in the image | Make sure `VAE_TILING` is `0` (the default). |
| Pasted commands do not work | Windows commands (PowerShell) do not work on Linux and the other way around. Put each option before the image name, and nothing (not even a space) after a trailing `\` or `` ` ``. |