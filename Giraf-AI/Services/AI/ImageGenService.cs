using System.Text;
using System.Text.Json;
using Giraf_AI.Classes;
using Giraf_AI.Classes.ImageProvider;

namespace Giraf_AI.Services.AI;

public class ImageGenService
{
    public ImageGenService(IHttpClientFactory httpClientFactory)
    {
        _imageGenerationClient = httpClientFactory.CreateClient("ImageGeneration");
    }
    
    private readonly HttpClient _imageGenerationClient;
    
    public async Task<List<string>> GenerateImageBase64Async(string request)
    {
        var imageGenerateRequest = new ImageProviderReq
        {
            Prompt = request,
            NegativePrompt = "explicit images",
            Size = "512x512",
            NumberImages = 3,
            InferenceSteps = 20,
            GuidanceScale = 7.5,
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

        // Validating that the model returns at least one image
        if (result?.Data == null || result.Data.Count == 0)
        {
            throw new InvalidOperationException("Image generation returned no images.");
        }

        // It extracts and returns all non-empty base64 image strings
        return result.Data
            .Where(item => !string.IsNullOrWhiteSpace(item.B64Json))
            .Select(item => item.B64Json)
            .ToList();
    }
}