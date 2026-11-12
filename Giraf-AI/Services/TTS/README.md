# TTS queue (Plapre)

The TTS pipeline follows the CORE -> AI API -> Text Synthesizer request/response sequence.

- CORE sends `text` (and may send `userPreferences`; preference handling is not implemented yet).
- AI queues the request in memory, with **at most 1,000 outstanding requests** (queued + processing).
- One worker sends the text to Plapre and converts its PCM output into WAV bytes.
- The HTTP request waits for the result. **No TTS database or local WAV file is used.**
- When the queue is full, the API immediately returns **503 Service Unavailable** with `Retry-After: 10`.
- When CORE disconnects or times out, the job is cancelled or skipped to avoid wasted generation.

## Endpoints

### CORE-compatible JSON response

`POST /api/v1/tts`

Request:

```json
{"text":"Hej verden","language":"da","format":"wav"}
```

Response (200):

```json
{"audio_base64":"UklGR..."}
```

The `audio_base64` field contains a complete WAV file encoded in base64. This matches the existing CORE `GirafAIClient.generate_tts` contract. The `language` and `format` request fields are currently ignored; only `text` is used.

### Direct WAV response

`POST /api/tts/generate`

Request:

```json
{"text":"Hej verden"}
```

Response (200): `Content-Type: audio/wav`, with WAV file bytes.

## Test locally (PowerShell)

Start Plapre Docker and run GIRAF AI on port 5000. From a second terminal:

```powershell
$body = @{ text = "Hej verden" } | ConvertTo-Json
Invoke-WebRequest -Uri "http://localhost:5000/api/tts/generate" -Method POST -ContentType "application/json" -Body $body -OutFile "speech.wav"
```

For the CORE-compatible response:

```powershell
$body = @{ text = "Hej verden"; language = "da"; format = "wav" } | ConvertTo-Json
$result = Invoke-RestMethod -Uri "http://localhost:5000/api/v1/tts" -Method POST -ContentType "application/json" -Body $body
[IO.File]::WriteAllBytes("core-speech.wav", [Convert]::FromBase64String($result.audio_base64))
```

The above commands save test files **on your own computer only**. The API itself does not persist audio.

## Test queue capacity

In the terminal where GIRAF AI starts:

```powershell
$env:Tts__MaxOutstandingJobs = "2"
dotnet run --no-launch-profile -- --urls http://localhost:5000
```

In another terminal, run `docker pause plapre` to prevent the active job from completing. Send three POST requests concurrently to `/api/tts/generate`. The first two wait; the third gets 503. Then run `docker unpause plapre`.

Stop GIRAF AI and run `Remove-Item Env:Tts__MaxOutstandingJobs` before restarting to restore the default limit of 1,000.

## Limitations

- The queue is in memory and is lost on restart.
- The request stays open until synthesis completes. CORE currently uses a 60-second HTTP timeout, so long queue waits can time out. This needs coordination with the CORE team for production.
- The current request DTO accepts `text`; user preference mapping to Plapre voices is not implemented.
- The existing image-generation database configuration in `Program.cs` is unrelated to TTS and remains unchanged.
- The old asynchronous `/api/tts/jobs`, job-status and stored-audio endpoints have been removed because they relied on persistence or retrieval after the original request.
