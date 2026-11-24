// Exposes text-to-speech requests while keeping provider work asynchronous through the queue.
using Giraf_AI.Data;
using Giraf_AI.DTOs;
using Giraf_AI.Jobs;
using Giraf_AI.Models;
using Giraf_AI.Services.Queue;
using Microsoft.AspNetCore.Mvc;

namespace Giraf_AI.Controllers;

/// <summary>
/// API controller responsible for handling text-to-speech requests.
/// </summary>
[ApiController]
[Route("api/tts")]
public class TtsController(
    IQueueService<TtsJob> queueService,
    AppDbContext dbContext) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> GenerateTts([FromBody] TtsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var originApp = request.OriginApp
                ?? throw new ArgumentException("OriginApp is required.");
            var language = string.IsNullOrWhiteSpace(request.Lang)
                ? "da"
                : request.Lang.Trim().ToLowerInvariant();

            var normalizedRequest = new TtsRequest
            {
                Text = request.Text,
                TtsID = request.TtsID,
                Lang = language,
                OriginApp = originApp
            };

            var queueEntry = new TtsQueue
            {
                Prompt = request.Text,
                TtsId = request.TtsID,
                Lang = language switch
                {
                    "da" => LangType.da,
                    "en" => LangType.en,
                    _ => throw new InvalidOperationException("The request contains an unsupported language.")
                },
                OriginApp = originApp,
                Received = DateTime.UtcNow
            };

            dbContext.TtsQueues.Add(queueEntry);
            await dbContext.SaveChangesAsync(cancellationToken);

            var job = new TtsJob
            {
                Request = normalizedRequest,
                TtsId = queueEntry.TtsId
            };
            await queueService.AddToQueue(job, cancellationToken);
            return Accepted();
        }
        
        // Handle cancellation requests gracefully by returning an empty result
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new EmptyResult();
        }
    }
}
