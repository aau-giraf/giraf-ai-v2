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
        
        // Find the queueEntry to store data in the current "row" or task
        var queueEntry = await _dbContext.ImgGenQueues.FindAsync(queueId)
                         ?? throw new InvalidOperationException("Queue entry was not found.");

        // Write the translation to the Database
        queueEntry.TranslatedPrompt = translation;
        await _dbContext.SaveChangesAsync();
        
        // Send the translation to the LLM NSFW content filter
        string contentFilter = await _llmService.GenerateLlmAsync(translation, "Prompts/NSFW/Master.txt");
        Console.WriteLine(contentFilter);
        
        // Sets the isNsfw flag if the output contains inappropriate
        var isNsfw = contentFilter.Contains( 
            "inappropriate", 
            StringComparison.OrdinalIgnoreCase);
        
        // Update the nsfw flag in the database
        queueEntry.Nsfw = isNsfw;
        await _dbContext.SaveChangesAsync();
        
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
        
        // Save changes to the queue entry in the database
        queueEntry.ImgData = imgData;
        queueEntry.TitleDa = titleDa;
        queueEntry.TitleEn = titleEn;
        queueEntry.Nsfw = false;
        queueEntry.Finished = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        
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