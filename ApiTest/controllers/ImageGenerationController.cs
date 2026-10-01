using Microsoft.AspNetCore.Mvc;

namespace DefaultNamespace;

[ApiController]
[Route("api/image-generation")]
public class ImageGenerationController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    public ImageGenerationController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpPost]
    public IActionResult GenerateImage([FromBody] ImageRequest request)
    {
        return Ok(new
        {
            message = "Prompt received!",
            prompt = request.Prompt,
            aiModel = request.AiModel,
            userPreferences = request.UserPreferences
        });
    }

    [HttpPost("return-image")]
    public IActionResult ReturnImage()
    {
        var imagePath = Path.Combine(
            _environment.ContentRootPath,
            "controllers",
            "apple_pictogram.png");

        if (!System.IO.File.Exists(imagePath))
        {
            return Problem(
                detail: $"The image file could not be found at '{imagePath}'.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        return PhysicalFile(imagePath, "image/png", "apple_pictogram.png");
    }

    public class ImageRequest
    {
        public string Prompt { get; init; } = string.Empty;
        public string AiModel { get; init; } = string.Empty;
        public UserPreferences UserPreferences { get; init; } = new();
    }

    public class UserPreferences
    {
        public string Realism { get; init; } = string.Empty;
        public string Colored { get; init; } = string.Empty;
    }
}