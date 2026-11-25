// Coordinates image requests, persistence, and the asynchronous image queue.
using Giraf_AI.DTOs;
using Giraf_AI.Models;
using Giraf_AI.Services.AI;

namespace Giraf_AI.Services;

// <summary>
// Service responsible for processing image generation requests, including translation, content filtering, and image generation.
// </summary>

/// <summary>
/// Coordinates image generation and its persisted result.
/// </summary>
public class ImageService
{
    // Private readonly fields for the LLM service and the image generation service
    private readonly LlmService _llmService;
    private readonly ImageGenService _imageGenerationService;

    public ImageService(
        // Inject the LLM service and the image generation service into the constructor
        LlmService llmService,
        ImageGenService imageGenerationService)
    {
        // Assign the injected services to the private fields
        _llmService = llmService;
        _imageGenerationService = imageGenerationService;
    }

    public async Task<ImageResult> ProcessImageRequestAsync(ImageRequest req, Action onContentCheckPassed)
    {
        // Normalize the origin app to lowercase for consistent processing
        var originApp = req.OriginApp.Trim().ToLowerInvariant()
            ?? throw new InvalidOperationException("The request contains no origin app.");
        var idType = req.IdType.Trim().ToLowerInvariant();

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
        
        // Close the TCP connection to FP / VTA and signal that the content check has passed and they can expect a response
        onContentCheckPassed();
        
        // Determine the correct title master prompt based on the origin app
        var titleMasterPromptDa = originApp switch
        {
            "vta" => "Prompts/TITLE/TitleDaVTA.txt",
            "fp" => "Prompts/TITLE/TitleDaFP.txt",
            _ => throw new InvalidOperationException("The request contains an unsupported origin app.")
        };
        var titleMasterPromptEn = originApp switch
        {
            "vta" => "Prompts/TITLE/TitleEnVTA.txt",
            "fp" => "Prompts/TITLE/TitleEnFP.txt",
            _ => throw new InvalidOperationException("The request contains an unsupported origin app.")
        };
        
        // Simultaneously start Image Generation, TitleDa and TitleEn and wait for all to finish.
        var results = await Task.WhenAll(
            _imageGenerationService.GenerateImageData(translation),
            _llmService.GenerateLlmAsync(req.Prompt, titleMasterPromptDa), 
            _llmService.GenerateLlmAsync(translation, titleMasterPromptEn));
        
        // Set the results to the desired variables
        var imgData = results[0];
        var titleDa = results[1];
        var titleEn = results[2];
        
        // This is the return body for GIRAF CORE
        return new ImageResult
        {
            TitleDa = titleDa,
            TitleEn = titleEn,
            ImgId = req.ImgID,
            OriginApp = originApp,
            // Only fill in the correct ID. The ID's with null will not be present in the return body
            CitizenId = idType == "citizenid" ? req.MapID : null,
            ClassId = idType == "classid" ? req.MapID : null,
            OrganizationId = idType == "organizationid" ? req.MapID : null,
            ImageData = imgData
        };
    }
}