// Exposes the image-generation endpoint and translates queued work into the API response contract.
using Giraf_AI.Data;
using Giraf_AI.DTOs;
using Giraf_AI.Jobs;
using Giraf_AI.Models;
using Giraf_AI.Services;
using Giraf_AI.Services.Queue;
using Microsoft.AspNetCore.Mvc;

namespace Giraf_AI.Controllers;

/// <summary>
/// API controller responsible for handling image generation requests.
/// </summary>
[ApiController]
[Route("api/image-generation")]
public class ImageGenerationController(
    IImageQueueService imageQueueService,
    AppDbContext dbContext) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> GenerateImage([FromBody] ImageRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var originApp = request.OriginApp.Trim().ToLowerInvariant();
            var normalizedRequest = new ImageRequest
            {
                Prompt = request.Prompt,
                ImgID = request.ImgID,
                MapID = request.MapID,
                IdType = request.IdType,
                OriginApp = originApp
            };

            var queueEntry = new ImgGenQueue
            {
                Prompt = request.Prompt,
                ImageId = request.ImgID,
                MapId = request.MapID,
                IdType = request.IdType.Trim().ToLowerInvariant() switch
                {
                    "citizenid" => IdType.citizen_id,
                    "classid" => IdType.class_id,
                    "organizationid" => IdType.organization_id,
                    _ => throw new InvalidOperationException("The request contains an unsupported ID type.")
                },
                OriginApp = originApp switch
                {
                    "vta" => "vta",
                    "fp" => "fp",
                    _ => throw new InvalidOperationException("The request contains an unsupported origin app.")
                },
                Received = DateTime.UtcNow
            };

            dbContext.ImgGenQueues.Add(queueEntry);
            await dbContext.SaveChangesAsync(cancellationToken);

            var job = new ImageJob
            {
                Request = normalizedRequest,
                QueueId = queueEntry.QueueId
            };
            await imageQueueService.AddToQueue(job, cancellationToken);
            await job.Completion.Task.WaitAsync(cancellationToken);
            return Accepted();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new EmptyResult();
        }
        catch (ContentFilterViolationException)
        {
            return StatusCode(StatusCodes.Status403Forbidden, "The prompt contains inappropriate content.");
        }

    }
}