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
    ITtsQueueService queueService,
    AppDbContext dbContext) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> GenerateTts([FromBody] TtsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var queueEntry = new TtsQueue
            {
                Prompt = request.Prompt,
                TtsId = request.TtsID,
                Lang = request.Lang switch
                {
                    "da" => LangType.da,
                    "en" => LangType.en,
                    _ => throw new InvalidOperationException("The request contains an unsupported language.")
                },
                Received = DateTime.UtcNow
            };

            dbContext.TtsQueues.Add(queueEntry);
            await dbContext.SaveChangesAsync(cancellationToken);

            var job = new TtsJob
            {
                Request = request,
                QueueId = queueEntry.QueueId
            };
            await queueService.AddToQueue(job, cancellationToken);
            return Accepted();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new EmptyResult();
        }
    }
}
