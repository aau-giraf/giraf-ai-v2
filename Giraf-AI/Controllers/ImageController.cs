using Microsoft.AspNetCore.Mvc;

namespace DefaultNamespace;

/// <summary>
/// API controller responsible for handling image generation requests.
/// </summary>
[ApiController]
[Route("api/image-generation")]
public class ImageGenerationController : ControllerBase
{
    private readonly ImageService imageService;
     
         public ImageGenerationController(ImageService imageService)
         {
             this.imageService = imageService;
         }

    /// <summary>
    /// Recevies the image generation request and returns the generated result
    /// </summary>
    /// <param name="request">Includes the user-provided image generation parameters (prompt)</param>
    /// <returns>
    /// The generated image data (base64) and temporary title information
    /// This return will be updated once LLM title generatoin and server image storing are implemented
    /// </returns>
    [HttpPost]
    public async Task<IActionResult> GenerateImage(
        [FromBody] GenerateImageRequest request)
    {
        var result = await this.imageService.GenerateImageAsync(request);

        return Ok(result);
    }
}