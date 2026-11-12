# Plapre TTS API — local test guide

This is the first of two planned TTS providers. The C# API currently selects
Plapre as the default, through the `ITtsProvider` interface. A second engine
can implement the same interface later. Queueing is deliberately postponed.

## Prerequisites

1. Start Docker Desktop.
2. From `Giraf-AI/TTSPlapre/TTSplapre`, run `docker compose up -d`.
3. Confirm `Invoke-RestMethod http://localhost:8000/health` returns `status: ok`.
4. Start the existing GIRAF C# API in Rider. The existing `Program.cs`
   requires the application's PostgreSQL connection configuration, even
   though the initial TTS pipeline doesn't yet use the database.

## Try text-to-speech

In PowerShell, replace port `5000` with the HTTP port printed by Rider:

```powershell
$response = Invoke-RestMethod -Uri "http://localhost:5000/api/tts/generate" -Method POST -ContentType "application/json" -Body (@{text="Hej, dette er en test."} | ConvertTo-Json)
$response
```

Download and play the generated file:

```powershell
Invoke-WebRequest -Uri ("http://localhost:5000" + $response.audioUrl) -OutFile "giraf-test.wav"
Invoke-Item "giraf-test.wav"
```

The WAV is also saved locally to `Giraf-AI/GeneratedAudio/<id>.wav`.

## Configuration

Defaults are `Tts:DefaultProvider=plapre` and
`Tts:PlapreBaseUrl=http://localhost:8000/`.
These can be overridden with environment variables
`Tts__DefaultProvider` and `Tts__PlapreBaseUrl` or a configuration file.
When the C# API itself runs in a Docker container, `localhost` will refer
to that container; point the base URL to the Plapre service instead.

## Future work

- Add a second `ITtsProvider` implementation and register it in `Program.cs`.
- Add queueing, a worker, durable status tracking, retries and concurrency controls.
- Apply authorization, text moderation, retention limits and access controls
  before deploying the API for child-facing use.
- Write unit/integration tests for provider selection, WAV output and persistence.

The initial endpoint waits for synthesis to finish: it is not a queued API.
