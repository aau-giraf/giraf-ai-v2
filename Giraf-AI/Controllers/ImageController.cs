using Giraf_AI.Classes;
using Giraf_AI.Services;
using Microsoft.AspNetCore.Mvc;

namespace Giraf_AI.Controllers;

/// <summary>
/// API controller responsible for handling image generation requests.
/// </summary>
[ApiController]
[Route("api/image-generation")]
public class ImageGenerationController(ImageService imageService) : ControllerBase
{
    /// <summary>
    /// Receives the image generation request and returns the generated result
    /// </summary>
    /// <param name="req">Includes the user-provided image generation parameters (prompt)</param>
    /// <returns>
    /// The generated image data (base64) and temporary title information
    /// This return will be updated once LLM title generation and server image storing are implemented
    /// </returns>
    [HttpPost]
    public async Task<IActionResult> GenerateImage([FromBody] ImageReq request)
    {
        var result = await imageService.ProcessImageRequestAsync(request);

        return Ok(result);
    }
}