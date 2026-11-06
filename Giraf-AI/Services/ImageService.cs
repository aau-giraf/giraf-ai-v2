using System.Text;
using System.Text.Json;
using Giraf_AI.DTOs;
using Giraf_AI.Models;
using Giraf_AI.Services.AI;
using System;

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
        var translation = await _llmService.GenerateLlmAsync(req.Prompt, "Prompts/TRANSLATOR/Master.txt");
        Console.WriteLine(translation);
        string contentFilter = await _llmService.GenerateLlmAsync(translation, "Prompts/NSFW/Master.txt");
        Console.WriteLine(contentFilter);
        if (contentFilter.Contains("inappropriate", StringComparison.OrdinalIgnoreCase)) // If the prompt is inappropriate send this instead of generating an image.
        {
            throw new ContentFilterViolationException();
        }

        var ImgData = await _imageGenerationService.GenerateImageData(translation);
        var titleDa = await _llmService.GenerateLlmAsync(translation,"Prompts/TITLE/TitleDa.txt");
        Console.WriteLine(titleDa);
        var titleEn = await _llmService.GenerateLlmAsync(translation,"Prompts/TITLE/TitleEn.txt");
        Console.WriteLine(titleEn);
        return new ImageResult
        {
            ImageData = ImgData,
            TitleDa = titleDa,
            TitleEn = titleEn,
            ImgID = req.ImgID,
            CitizenID = 69, // placeholder
            ClassID = 69, // placeholder
            CrganizationID = 69 // placeholder
        };
    }
}