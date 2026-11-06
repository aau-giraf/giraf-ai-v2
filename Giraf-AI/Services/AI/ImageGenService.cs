﻿using System.Text;
using System.Text.Json;
using Giraf_AI.DTOs.Providers.ImageProvider;
using Giraf_AI.HelperClasses;

namespace Giraf_AI.Services.AI;

public class ImageGenService
{
    public ImageGenService(IHttpClientFactory httpClientFactory)
    {
        _imageGenerationClient = httpClientFactory.CreateClient("ImageGeneration");
    }
    
    private readonly HttpClient _imageGenerationClient;
    
    public async Task<string> GenerateImageData(string request)
    {
        var imageGenerateRequest = new ImageProviderRequest
        {
            Prompt = new ReadFromFile(request, false, "Prompts/IMGGEN/IMG_Master.txt").Text,
            NegativePrompt = await File.ReadAllTextAsync("Prompts/IMGGEN/IMG_Negative.txt"),
            Size = "512x512",
            NumberImages = 1,
            InferenceSteps = 8,
            GuidanceScale = 1.0,
            Seed = -1
        };

        // Send request to the model container endpoint
        using var response = await _imageGenerationClient.PostAsJsonAsync("v1/images/generations", imageGenerateRequest);

        // Throws error if container returns an error
        response.EnsureSuccessStatusCode();

        // Deserializes the model container's response into ImageProviderResponse DTO
        var result = await response.Content.ReadFromJsonAsync<ImageProviderResponse>() 
                     ?? throw new InvalidOperationException("Image Generator returned an empty response.");

        // Validate that the model returned one non-empty image
        var image = result?.Data
            .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.B64Json));

        if (image == null)
        {
            throw new InvalidOperationException("Image generation returned no images.");
        }

        return image.B64Json;
    }
}