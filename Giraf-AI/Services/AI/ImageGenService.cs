using System.Text;
using System.Text.Json;
using Giraf_AI.DTOs.Providers.ImageProvider;

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
        var imageGenerateRequest = new ImageProviderReq
        {
            Prompt = request,
            NegativePrompt = "explicit images",
            Size = "512x512",
            NumberImages = 1,
            InferenceSteps = 8,
            GuidanceScale = 1.0,
            Seed = -1
        };
        // Serializes the request into JSON for the model container
        var body = JsonSerializer.Serialize(imageGenerateRequest);
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        // Send request to the model container endpoint
        using var response = await _imageGenerationClient.PostAsync("v1/images/generations", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        // Throws error if container returns an error
        response.EnsureSuccessStatusCode();

        // Deserializes the model container's response into ImageProviderResponse DTO
        var result = JsonSerializer.Deserialize<ImageProviderRes>(responseBody);

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