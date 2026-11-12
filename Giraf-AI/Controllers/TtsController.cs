using Giraf_AI.DTOs;
using Giraf_AI.Services.TTS;
using Microsoft.AspNetCore.Mvc;

namespace Giraf_AI.Controllers;

[ApiController]
[Route("api/tts")]
public sealed class TtsController(
    TtsQueueService queue,
    IAudioStorage audioStorage,
    ILogger<TtsController> logger) : ControllerBase
{
    /// <summary>
    /// Enqueues speech generation and returns immediately with a job ID.
    /// </summary>
    [HttpPost("jobs")]
    public IActionResult Enqueue([FromBody] TtsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest("Text cannot be empty.");
        }

        if (!queue.TryEnqueue(request.Text, waitForCompletion: false, out TtsQueueItem job))
        {
            return QueueFull();
        }

        return AcceptedAtAction(
            nameof(GetJob),
            new { id = job.Id },
            queue.GetStatus(job.Id));
    }

    /// <summary>Returns queued, processing, completed, or failed status.</summary>
    [HttpGet("jobs/{id:guid}")]
    public IActionResult GetJob(Guid id)
    {
        TtsJobStatus? status = queue.GetStatus(id);
        return status is null ? NotFound() : Ok(status);
    }

    /// <summary>
    /// Backwards-compatible synchronous endpoint. It uses the same queue to
    /// prevent concurrent requests from bypassing the single Plapre worker.
    /// </summary>
    [HttpPost("generate")]
    public async Task<IActionResult> Generate(
        [FromBody] TtsRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest("Text cannot be empty.");
        }

        if (!queue.TryEnqueue(request.Text, waitForCompletion: true, out TtsQueueItem job))
        {
            return QueueFull();
        }

        try
        {
            TtsResult result = await job.Completion!.Task.WaitAsync(cancellationToken);
            return Ok(result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Disconnecting the client does not cancel a job already in the queue.
            return new EmptyResult();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Synchronous TTS job {JobId} failed.", job.Id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                "Speech generation failed.");
        }
    }

    private IActionResult QueueFull()
    {
        // A hint to clients; it does not guarantee that capacity is free in 10 seconds.
        Response.Headers["Retry-After"] = "10";
        return StatusCode(
            StatusCodes.Status503ServiceUnavailable,
            $"TTS queue is full (maximum {queue.MaxOutstandingJobs} outstanding requests). Try again later.");
    }

    [HttpGet("{id:guid}/audio")]
    public IActionResult GetAudio(Guid id)
    {
        string path = audioStorage.GetFilePath(id);
        if (!System.IO.File.Exists(path))
        {
            return NotFound();
        }

        return PhysicalFile(path, "audio/wav");
    }
}
