    # Kokoro TTS API

A Dockerized text-to-speech API powered by
[Kokoro-82M](https://huggingface.co/hexgrad/Kokoro-82M).

The API accepts text through an HTTP request and returns the generated
speech as a WAV audio file. The Kokoro model runs on the server, so
client devices do not need to install or run the model.

## Features

-   Text-to-speech generation with Kokoro
-   HTTP API built with FastAPI
-   WAV audio responses
-   Docker-based deployment
-   CPU-based inference

## Requirements

-   Docker Desktop or Docker Engine
-   Internet connection for the initial model download

Python does not need to be installed on client devices.

## Project structure

    .
    ├── app/
    │   ├── main.py
    │   └── tts.py
    ├── Dockerfile
    ├── requirements.txt
    ├── .dockerignore
    └── README.md

## Setup

### 1. Build the Docker image

Run this command from the project directory:

    docker build -t kokoro-tts .

### 2. Start the container

    docker run -d -p 8000:8000 --name kokoro-server kokoro-tts

The API will be available at:

    http://localhost:8000

On its first startup, Kokoro downloads the required model files from
Hugging Face. This requires an internet connection and may take a few
minutes.

## API endpoints

  Method   Endpoint    Description
  -------- ----------- ---------------------------------------------------
  GET      `/health`   Checks whether the API is running.
  POST     `/tts`      Converts text into speech and returns a WAV file.

### Health check

Request:

    GET /health

Example response:

    {
      "status": "healthy"
    }

### Generate speech

Send a POST request to `/tts` with a JSON body:

    {
      "text": "Hello! This is a test of Kokoro text to speech.",
      "voice": "af_heart"
    }

The `text` field is required. The `voice` field is optional and defaults
to `af_heart`.

The response is a WAV audio file with the content type `audio/wav`.

## API documentation

FastAPI provides an interactive interface for testing the endpoints.

Start the container, then open:

<http://localhost:8000/docs>

Expand `POST /tts`, select **Try it out**, enter the JSON body, and
select **Execute**.

## Access from another device

Devices on the same local network can use the API by connecting to the
computer running Docker.

1.  On the server computer, run `ipconfig` in PowerShell.
2.  Find the IPv4 address for the active network connection.
3.  Use that address instead of `localhost` in the API URL.
4.  Allow incoming connections on port `8000` in the computer's firewall
    if necessary.

For example, if the server's IPv4 address is `192.168.1.100`, the TTS
endpoint is:

    http://192.168.1.100:8000/tts

Use the actual IPv4 address of the server; the address above is only an
example.

## Docker commands

Check running containers:

    docker ps

View the application logs:

    docker logs kokoro-server

Stop the container:

    docker stop kokoro-server

Start the existing container:

    docker start kokoro-server

Remove the container:

    docker rm -f kokoro-server

### Rebuild after making changes

Remove the old container, rebuild the image, and start a new container:

    docker rm -f kokoro-server
    docker build -t kokoro-tts .
    docker run -d -p 8000:8000 --name kokoro-server kokoro-tts

## How it works

1.  A client sends a POST request containing text and an optional voice.
2.  FastAPI receives the request.
3.  The application passes the text to the Kokoro model.
4.  Kokoro generates the speech.
5.  The API returns the audio as a WAV file.

The model runs on the Docker host. Client devices only need to send HTTP
requests and play the returned audio.

## Notes

-   The Docker image uses CPU-based PyTorch.
-   The Kokoro model is loaded when the application starts and reused
    for later requests.
-   The initial model download requires internet access.
-   Generated audio is returned in the HTTP response and is not saved
    permanently by the current implementation.
