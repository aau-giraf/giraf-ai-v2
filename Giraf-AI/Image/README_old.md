# Example usage of this docker container using the stable diffusion model SD3.5 turbo.

Prerequisites for this guide:

1. Run windows or Linux.
2. Have docker installed.

Create directory (folder) anywhere on the pc and using a terminal navigate to that directory.

You need to have a valid huggingface login and have accepted the terms to use the specified model on their website.

Once done run this command:

huggingface-cli login

If huggingface is not installed install using:

Windows:
winget install Python.Python.3.12
python -m pip install -U huggingface_hub

Linux:
sudo apt update
sudo apt install -y python3 python3-pip python3-venv
python3 -m pip install -U huggingface_hub

When both python and huggingface is installed and logged into download the model using this command:

python -c "from huggingface_hub import snapshot_download; snapshot_download(repo_id='stabilityai/stable-diffusion-3.5-large-turbo', local_dir='stable-diffusion-3.5-large-turbo', local_dir_use_symlinks=False)"

NOTE: This will download the model into a new folder called stable-diffusion-3.5-large-turbo inside the current folder.

Open terminal and cd to folder with app.py and Dockerfile. Then run this command:

docker run --gpus all -p PORT:PORT `
    -v "C:\PATH\TO\MODEL\FOLDER:/models/MODEL_NAME_HERE" `
    name-of-image

example with stable-diffusion-3.5-large-turbo:

docker run --gpus all -p 8000:8000 `
    -v "C:\Users\ulrik\stable-diffusion-3.5-large-turbo:/models/stable-diffusion-3.5-large-turbo" `
    sd35-api

Once the image has been created run it then wait until it says "Model ready."

Try to generate an image with this command:

$body = @{
prompt = "apple"
negative_prompt = ""
size = "512x512"
n = 1
num_inference_steps = 20
guidance_scale = 6.0
seed = -1
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:8000/v1/images/generations" `
-Method Post -ContentType "application/json" -Body $body -TimeoutSec 300

$bytes = [Convert]::FromBase64String($response.data[0].b64_json)
[IO.File]::WriteAllBytes("$PWD\generated.png", $bytes)
Write-Host "Saved: $PWD\generated.png"