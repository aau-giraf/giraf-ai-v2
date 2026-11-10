using System.Text;
using System.Text.Json;
using Giraf_AI.DTOs;
using Giraf_AI.Models;
using Giraf_AI.Services.AI;
using System;
using Giraf_AI.Data;

namespace Giraf_AI.Services;

public class ImageService
{
    private readonly LlmService _llmService;
    private readonly ImageGenService _imageGenerationService;
    private readonly AppDbContext _dbContext;

    public ImageService(
        LlmService llmService,
        ImageGenService imageGenerationService,
        AppDbContext dbContext)
    {
        _llmService = llmService;
        _imageGenerationService = imageGenerationService;
        _dbContext = dbContext;
    }

    public async Task<ImageResult> ProcessImageRequestAsync(ImageRequest req, int queueId)
    {
        // Send the prompt to the LLM translator
        var translation = await _llmService.GenerateLlmAsync(req.Prompt, "Prompts/TRANSLATOR/Master.txt");
        Console.WriteLine(translation);
        
        // Write the translation to the Database
        var queueEntry = await _dbContext.ImgGenQueues.FindAsync(queueId)
                         ?? throw new InvalidOperationException("Queue entry was not found.");

        queueEntry.TranslatedPrompt = translation;

        await _dbContext.SaveChangesAsync();
        
        // Send the translation to the LLM NSFW content filter
        string contentFilter = await _llmService.GenerateLlmAsync(translation, "Prompts/NSFW/Master.txt");
        Console.WriteLine(contentFilter);
        if (contentFilter.Contains("inappropriate", StringComparison.OrdinalIgnoreCase)) // If the prompt is inappropriate send this instead of generating an image.
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