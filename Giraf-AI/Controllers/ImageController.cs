using Giraf_AI.Data;
using Giraf_AI.DTOs;
using Giraf_AI.Jobs;
using Giraf_AI.Models;
using Giraf_AI.Services.Queue;
using Microsoft.AspNetCore.Mvc;

namespace Giraf_AI.Controllers;

/// <summary>
/// API controller responsible for handling image generation requests.
/// </summary>
[ApiController]
[Route("api/image-generation")]
public class ImageGenerationController(
    IQueueService queueService,
    AppDbContext dbContext) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> GenerateImage([FromBody] ImageRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var queueEntry = new ImgGenQueue
            {
                Prompt = request.Prompt,
                ImageId = request.ImgID,
                MapId = request.MapID,
                IdType = request.IdType switch
                {
                    "CitizenID" => IdType.citizen_id,
                    "ClassID" => IdType.class_id,
                    "OrganizationID" => IdType.organization_id,
                    _ => throw new InvalidOperationException("The request contains an unsupported ID type.")
                },
                Received = DateTime.UtcNow
            };

            dbContext.ImgGenQueues.Add(queueEntry);
            await dbContext.SaveChangesAsync(cancellationToken);

            var job = new ImageJob
            {
                Request = request
            };
            await queueService.AddToQueue(job, cancellationToken);
            var result = await job.Completion.Task.WaitAsync(cancellationToken);
            return Ok(result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new EmptyResult();
        }

        //var result = await imageService.ProcessImageRequestAsync(request);

        //return Ok(result);
    }
}