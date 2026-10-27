using Giraf_AI.DTOs;
using Giraf_AI.Jobs;
using Giraf_AI.Services.Queue;
using Microsoft.AspNetCore.Mvc;

namespace Giraf_AI.Controllers;

/// <summary>
/// API controller responsible for handling image generation requests.
/// </summary>
[ApiController]
[Route("api/image-generation")]
public class ImageGenerationController(IQueueService queueService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> GenerateImage([FromBody] ImageReq request, CancellationToken cancellationToken)
    {
		var job = new ImageJob {
			Request = request
		};
		
        try
        {
            await queueService.AddToQueue(job, cancellationToken);
            ImageRes result = await job.Completion.Task.WaitAsync(cancellationToken);
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