using Microsoft.AspNetCore.Mvc;

namespace DefaultNamespace;

[ApiController]
[Route("api/image-generation")]
public class ImageGenerationController : ControllerBase
{
    [HttpPost]
    public IActionResult GenerateImage([FromBody] ImageClass.ImageGenerateRequest request)
    {
        return Ok(new ImageClass.ImageGenerateResponse
        {
            Path = "generated-image.jpg",
        });
    }
}