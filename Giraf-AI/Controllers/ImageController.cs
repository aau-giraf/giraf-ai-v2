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
    IQueueService<ImageJob> imgQueueService,
    AppDbContext dbContext) : ControllerBase
{
    // POST endpoint for generating images based on the provided request.
    [HttpPost]
    public async Task<IActionResult> GenerateImage([FromBody] ImageRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Normalize the origin app to lowercase for consistent processing
            var originApp = request.OriginApp.Trim().ToLowerInvariant()
                ?? throw new ArgumentException("OriginApp is required.");
            var idType = request.IdType.Trim().ToLowerInvariant();
            var normalizedRequest = new ImageRequest
            {
                Prompt = request.Prompt,
                ImgID = request.ImgID,
                MapID = request.MapID,
                IdType = idType,
                OriginApp = originApp
            };

            // Create a new ImgGenQueue entry to store the request in the database
            var queueEntry = new ImgQueue
            {
                Prompt = normalizedRequest.Prompt,
                ImageId = normalizedRequest.ImgID,
                MapId = normalizedRequest.MapID,
                IdType = normalizedRequest.IdType switch
                {
                    "citizenid" => IdType.citizen_id,
                    "classid" => IdType.class_id,
                    "organizationid" => IdType.organization_id,
                    _ => throw new InvalidOperationException("The request contains an unsupported ID type.")
                },
                OriginApp = normalizedRequest.OriginApp switch
                {
                    "vta" => OriginApp.vta,
                    "fp" => OriginApp.fp,
                    _ => throw new InvalidOperationException("The request contains an unsupported origin app.")
                },
                Received = DateTime.UtcNow
            };
            
            // Add the queue entry to the database and save changes
            dbContext.ImgGenQueues.Add(queueEntry);
            await dbContext.SaveChangesAsync(cancellationToken);

            // Create a new ImageJob to represent the queued work and add it to the image queue
            var job = new ImageJob
            {
                Request = normalizedRequest,
                ImageId = queueEntry.ImageId
            };
            await imgQueueService.AddToQueue(job, cancellationToken);
            await job.ContentCheck.Task.WaitAsync(cancellationToken);
            return Accepted();
        }
        
        // Handle cancellation requests gracefully by returning an empty result
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new EmptyResult();
        }
        
        // Handle content filter violations by returning a 403 Forbidden status code with an appropriate message
        catch (ContentFilterViolationException)
        {
            return StatusCode(StatusCodes.Status403Forbidden, "The prompt contains inappropriate content.");
        }

    }
}