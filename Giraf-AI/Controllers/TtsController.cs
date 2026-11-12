using Giraf_AI.DTOs;
using Giraf_AI.Services.TTS;
using Microsoft.AspNetCore.Mvc;

namespace Giraf_AI.Controllers;

[ApiController]
[Route("api/tts")]
public sealed class TtsController(
    TtsQueueService queue,
    ILogger<TtsController> logger) : ControllerBase
{
    /// <summary>
    /// Synchronous WAV endpoint for direct clients and local testing.
    /// All requests use the same bounded Plapre queue.
    /// </summary>
    [HttpPost("generate")]
    public Task<IActionResult> Generate(
        [FromBody] TtsRequest request,
        CancellationToken cancellationToken) =>
        GenerateQueuedAsync(request, cancellationToken, returnBase64: false);

    /// <summary>
    /// Compatibility endpoint for GIRAF CORE's existing GirafAIClient.
    /// CORE expects JSON with an audio_base64 field, not a file URL.
    /// The decoded bytes are a complete WAV file.
    /// </summary>
    [HttpPost("/api/v1/tts")]
    public Task<IActionResult> GenerateForCore(
        [FromBody] TtsRequest request,
        CancellationToken cancellationToken) =>
        GenerateQueuedAsync(request, cancellationToken, returnBase64: true);

    private async Task<IActionResult> GenerateQueuedAsync(
        TtsRequest request,
        CancellationToken cancellationToken,
        bool returnBase64)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest("Text cannot be empty.");
        }

        if (!queue.TryEnqueue(request.Text, cancellationToken, out TtsQueueItem job))
        {
            Response.Headers["Retry-After"] = "10";
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                $"TTS queue is full (maximum {queue.MaxOutstandingJobs} outstanding requests). Try again later.");
        }

        try
        {
            byte[] wav = await job.Completion.Task.WaitAsync(cancellationToken);

            // Existing CORE client reads audio_base64 and decodes it into audio bytes.
            // The binary endpoint avoids base64 overhead for other consumers.
            if (returnBase64)
            {
                return Ok(new { audio_base64 = Convert.ToBase64String(wav) });
            }

            return File(wav, "audio/wav", $"{job.Id:N}.wav");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The caller disconnected or timed out; the worker cancels this job.
            return new EmptyResult();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "TTS job {JobId} failed.", job.Id);
            return StatusCode(StatusCodes.Status502BadGateway,
                "Speech generation failed.");
        }
    }
}
