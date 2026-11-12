using Giraf_AI.DTOs;
using Giraf_AI.Services.AI;

namespace Giraf_AI.Services;

public class ImageService
{
    private readonly LlmService _llmService;
    private readonly ImageGenService _imageGenerationService;

    public ImageService(
        LlmService llmService,
        ImageGenService imageGenerationService)
    {
        _llmService = llmService;
        _imageGenerationService = imageGenerationService;
    }

    public async Task<ImageResult> ProcessImageRequestAsync(ImageRequest req)
    {
        // Send the prompt to the LLM translator
        var translation = await _llmService.GenerateLlmAsync(req.Prompt, "Prompts/TRANSLATOR/Master.txt");
        Console.WriteLine(translation);

        // Send the translation to the LLM NSFW content filter
        string contentFilter = await _llmService.GenerateLlmAsync(translation, "Prompts/NSFW/Master.txt");
        Console.WriteLine(contentFilter);
        
        // Sets the isNsfw flag if the output contains inappropriate
        var isNsfw = contentFilter.Contains( 
            "inappropriate", 
            StringComparison.OrdinalIgnoreCase);
        
        // Throw the nsfw error to GIRAF CORE
        if (isNsfw)
        {
            throw new ContentFilterViolationException();
        }
        
        // Simultaneously start Image Generation, TitleDa and TitleEn and wait for all to finish.
        var results = await Task.WhenAll(
            _imageGenerationService.GenerateImageData(translation),
            _llmService.GenerateLlmAsync(translation, "Prompts/TITLE/TitleDa.txt"),
            _llmService.GenerateLlmAsync(translation, "Prompts/TITLE/TitleEn.txt"));
        
        // Set the results to the desired variables
        var imgData = results[0];
        var titleDa = results[1];
        var titleEn = results[2];
        
        // This is the return body for GIRAF CORE
        return new ImageResult
        {
            ImageData = imgData,
            TitleDa = titleDa,
            TitleEn = titleEn,
            ImgID = req.ImgID,
            // Only fill in the correct ID. The ID's with null will not be present in the return body
            CitizenID = req.IdType == "CitizenID" ? req.MapID : null,
            ClassID = req.IdType == "ClassID" ? req.MapID : null,
            OrganizationID = req.IdType == "OrganizationID" ? req.MapID : null
        };
    }
}