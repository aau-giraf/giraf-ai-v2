# Ollama in Docker — Setup Guide

This guide explains how to:

1. Install and run **Ollama** in Docker on Windows
2. Download an LLM
3. Check installed models
4. Send HTTP requests to the LLM
5. Get only the generated response

## Requirements

* Windows 10/11
* [Docker Desktop](https://www.docker.com/products/docker-desktop/)
* An NVIDIA GPU

---

# 1. Create an Ollama Docker Volume

Open **PowerShell** and run:

```powershell
docker volume create ollama
```

This creates a Docker volume named `ollama`.

The volume is used to store Ollama's models so that the models persist even if the Ollama container is stopped or recreated.

---

# 2. Start Ollama in Docker

## NVIDIA GPU

If you have an NVIDIA GPU and Docker Desktop is configured for GPU access, run:

```powershell
docker run -d `
  --gpus=all `
  -v ollama:/root/.ollama `
  -p 11434:11434 `
  --name ollama `
  ollama/ollama
```

The command:

* `-d` — runs the container in the background
* `--gpus=all` — gives the container access to all available NVIDIA GPUs
* `-v ollama:/root/.ollama` — stores Ollama's models in the `ollama` Docker volume
* `-p 11434:11434` — exposes Ollama's API on port `11434`
* `--name ollama` — gives the container the name `ollama`
* `ollama/ollama` — uses the official Ollama Docker image

The official Ollama Docker image documents this setup for NVIDIA GPUs.

---

# 3. Check if Ollama is Running

Run:

```powershell
docker ps
```

You should see a container named:

```text
ollama
```

For example:

```text
CONTAINER ID   IMAGE           STATUS         PORTS
xxxxxxxxxxxx   ollama/ollama   Up 10 seconds  0.0.0.0:11434->11434/tcp
```

If the `ollama` container is listed and its status starts with `Up`, Ollama is running.

---

# 4. Download an LLM

For example, to download **Llama 3.2 3B**, run:

```powershell
docker exec -it ollama ollama pull llama3.2:3b
```

This downloads the model into the Ollama data stored in the Docker volume.

The `docker exec` command executes the `ollama pull` command inside the running Ollama container.

---

# 5. Check Installed Models

To see which models have been downloaded, run:

```powershell
docker exec -it ollama ollama list
```

You should see something similar to:

```text
NAME           ID              SIZE      MODIFIED
llama3.2:3b    xxxxxxxxxxxx    ... GB    ...
```

---

# 6. Send an HTTP Request from PowerShell

Ollama provides an HTTP API on:

```text
http://localhost:11434
```

The `/api/generate` endpoint can be used to generate text from a model.

Create a PowerShell request like this:

```powershell
$body = @{
    model = "llama3.2:3b"
    prompt = "Where do giraffes live?"
    stream = $false
} | ConvertTo-Json

$response = Invoke-RestMethod `
    -Uri "http://localhost:11434/api/generate" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body

$response.response
```

The important part is:

```powershell
$response.response
```

Ollama's `/api/generate` endpoint returns a JSON object containing several pieces of information. The `response` property contains the actual generated text.

---

# 7. Send an HTTP Request from JetBrains Rider

You can also send the request directly from a `.http` file in Rider.

Create a file such as:

```text
ollama.http
```

Then add:

```http
POST http://localhost:11434/api/generate
Content-Type: application/json

{
  "model": "llama3.2:3b",
  "prompt": "Where do giraffes live?",
  "stream": false
}

> {%
    console.log(response.body.response);
%}
```

Run the request using Rider's HTTP Client.

The response handler:

```javascript
console.log(response.body.response);
```

extracts the `response` property from Ollama's JSON response and prints only the generated text to the HTTP Client console.

Instead of displaying the complete JSON response, you should get something like:

```text
Giraffes live primarily in Africa, particularly in savannas, grasslands, and open woodlands.
```

---

# 8. Using a Different Model

You can download other models from the Ollama model library.

For example:

```powershell
docker exec -it ollama ollama pull llama3.2:1b
```

or:

```powershell
docker exec -it ollama ollama pull llama3.1:8b
```

After downloading a model, change the `"model"` field in your HTTP request.

For example:

```json
{
  "model": "llama3.1:8b",
  "prompt": "Where do giraffes live?",
  "stream": false
}
```

You can see the models installed in your container with:

```powershell
docker exec -it ollama ollama list
```

---

# 9. Useful Docker Commands

### Stop Ollama

```powershell
docker stop ollama
```

### Start Ollama again

```powershell
docker start ollama
```

Your downloaded models remain available because they are stored in the `ollama` Docker volume.

### Restart Ollama

```powershell
docker restart ollama
```

### View Ollama logs

```powershell
docker logs ollama
```

### Follow the logs

```powershell
docker logs -f ollama
```

Press `Ctrl+C` to stop following the logs.

### Remove the container

```powershell
docker rm -f ollama
```

Removing the container **does not remove the `ollama` volume**, so your downloaded models remain stored.

You can create a new container using the same volume:

```powershell
docker run -d `
  --gpus=all `
```

