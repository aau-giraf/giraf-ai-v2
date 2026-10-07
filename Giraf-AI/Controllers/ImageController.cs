using Microsoft.AspNetCore.Mvc;

namespace DefaultNamespace;

[ApiController]
[Route("api/image-generation")]
public class ImageGenerationController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> GenerateImageV2([FromBody] ImageClass.ImageGenerateRequest request)
    {
        Task<string> imageTask = GenerateImageFile(request.Prompt, request.ImagePreferences);
        Task<ImageClass.ImageText> textTask = GenerateImageText(request.Prompt);
        
        await Task.WhenAll(imageTask, textTask);

        return Ok(new ImageClass.ImageGenerateResponse
        {
            Path = await imageTask,
            ImageText = await textTask
        });
    }

    public async Task<string> GenerateImageFile(string prompt, UserPreferencesClass.ImagePreferences imagePreferences)
    {
        return "";
    }
    
    public async Task<ImageClass.ImageText> GenerateImageText(string prompt)
    {
        return new ImageClass.ImageText
        {
            TitleDa = prompt,
            TitleEn = prompt,
            AltTextDa = prompt,
            AltTextEn = prompt
        };
    }

}