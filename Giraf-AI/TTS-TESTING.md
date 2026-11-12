# Plapre TTS pipeline and queue — local test guide

The C# API uses Plapre through the `ITtsProvider` interface. Both synchronous
and asynchronous endpoints use the same in-memory TTS queue, independently of
the existing image queue. The background worker processes **one job at a time**.

## Start locally

1. Start Docker Desktop.
2. From `Giraf-AI/TTSPlapre/TTSPlapre`, run `docker compose up -d`.
3. Check Plapre: `Invoke-RestMethod http://localhost:8000/health` → `ok`.
4. In a PowerShell terminal at `Giraf-AI`, configure the existing application
   database connection as appropriate (the app requires a connection string).
5. Run `dotnet run --no-launch-profile -- --urls http://localhost:5000`.

## Asynchronous queued pipeline

In Postman:

- **POST** `http://localhost:5000/api/tts/jobs`
- Body → raw → JSON: `{"text":"Hej, dette er en test af køen."}`
- Expect **202 Accepted** with a job ID and `status`: `queued` (or
  `processing` if the worker picked it up immediately). The `Location`
  header points to the job status endpoint.
- **GET** `http://localhost:5000/api/tts/jobs/{id}`
- Poll until `status` is `completed` or `failed`.
- On success, read `audioUrl`, then **GET**
  `http://localhost:5000{audioUrl}` to retrieve the WAV.

PowerShell example:

```powershell
$job = Invoke-RestMethod -Uri "http://localhost:5000/api/tts/jobs" -Method POST -ContentType "application/json" -Body (@{text="Hej fra Plapre-køen."} | ConvertTo-Json)
$job
Invoke-RestMethod "http://localhost:5000/api/tts/jobs/$($job.id)"
```

Once completed:

```powershell
$status = Invoke-RestMethod "http://localhost:5000/api/tts/jobs/$($job.id)"
Invoke-WebRequest ("http://localhost:5000" + $status.audioUrl) -OutFile "queued-speech.wav"
Invoke-Item "queued-speech.wav"
```

## Existing synchronous endpoint (still supported)

**POST** `http://localhost:5000/api/tts/generate` with
`{"text":"Hej, dette er en test."}` waits until the job completes and
returns `200 OK` with `id` and `audioUrl`. It now passes through the
same queue to avoid bypassing the worker.

## Queue behavior and limitations

- Bounded FIFO channel: at most 100 **waiting** jobs. The worker processes one
  additional job at a time.
- If the waiting queue is full, new requests receive **503 Service Unavailable**.
- States: `queued`, `processing`, `completed`, `failed`.
- A failed job does not stop the worker. Failures are logged; clients see a
  generic error message.
- Job statuses are in memory and are **lost on API restart**. Completed WAV
  files remain on disk. Finished status entries are pruned after 24 hours
  during later enqueue operations; audio files are **not** deleted automatically.
- No retries, cancellation endpoint, durable storage, authentication, or
  multiple workers yet. A disconnected synchronous client does not remove
  its queued job.
- Configure `Tts__PlapreBaseUrl` when Plapre is not available at
  `http://localhost:8000/`.

## Before production

Add authentication and authorization, content safety checks, durable job
storage and status recovery, cleanup/retention policies for generated audio,
resource limits, and integration/unit tests. This version is intended for
local development and testing, not a production-ready child-facing service.
