using Giraf_AI.DTOs;
using Giraf_AI.Services.TTS;
using Microsoft.AspNetCore.Mvc;

namespace Giraf_AI.Controllers;

[ApiController]
[Route("api/tts")]
public sealed class TtsController(
    TtsGenerationService generationService,
    IAudioStorage audioStorage,
    ILogger<TtsController> logger) : ControllerBase
{
    [HttpPost("generate")]
    public async Task<IActionResult> Generate(
        [FromBody] TtsRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest("Text cannot be empty.");
        }

        try
        {
            TtsResult result = await generationService.GenerateAsync(
                request.Text,
                cancellationToken);

            return Ok(result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new EmptyResult();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to generate speech.");

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "Speech generation failed.");
        }
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
