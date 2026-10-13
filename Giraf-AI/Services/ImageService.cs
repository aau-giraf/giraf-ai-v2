using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DefaultNamespace;

/// <summary>
/// Handles the entire image generation workflow: prepares the provider request,
/// calls the model container, and combines the image data with the temporary titles
/// </summary>
public class ImageService
{
    private readonly HttpClient imageGenerationClient;
    
    /// <summary>
    /// Initializes the service with an HttpClient for the image generation model container
    /// </summary>
    /// <param name="httpClientFactory">
    /// Factory is used instead of "new HttpClient()" because it allows
    /// for recycling of underlying connection handlers too avoid socket exhaustion
    /// </param>
    public ImageService(IHttpClientFactory httpClientFactory)
    {
        this.imageGenerationClient = httpClientFactory.CreateClient("ImageGeneration");
    }

    /// <summary>
    /// Generates an image based on the user request
    /// Combines model output (base64 image data) with the temporary title data
    /// </summary>
    /// <param name="request">Includes the prompt from the user</param>
    /// <returns>
    /// Returns the image data (base64) and temporary titles
    /// </returns>
    public async Task<GenerateImageResponse> GenerateImageAsync(GenerateImageRequest request)
    {
        var imageGenerateRequest = new ImageGenerationProviderRequest
        {
            Prompt = request.Prompt,
            NegativePrompt = "explicit images",
            Size = "512x512",
            NumberImages = 3,
            InferenceSteps = 20,
            GuidanceScale = 7.5,
            Seed = -1
        };

        var imageTask = GenerateImageBase64Async(imageGenerateRequest);
        var textTask = GenerateImageTextAsync(request.Prompt);

        // Waits for both imageTask and textTask to finish
        await Task.WhenAll(imageTask, textTask);

        return new GenerateImageResponse
        {
            Path = await imageTask, // Path currently returns base64 instead of path
            ImageText = await textTask
        };
    }
    
    /// <summary>
    /// Sends the image generation request to the model container and returns the generated image/images as base64 strings
    /// </summary>
    /// <param name="request">Includes the prompt and all model-specific parameters</param>
    /// <returns>A list of generated images encoded as base64 strings</returns>
    private async Task<List<string>> GenerateImageBase64Async( ImageGenerationProviderRequest request)
    {
        // Serializes the request into JSON for the model container
        var body = JsonSerializer.Serialize(request);
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        
        // Send request to the model container endpoint
        using var response = await this.imageGenerationClient.PostAsync("v1/images/generations", content);
        var responseBody = await response.Content.ReadAsStringAsync();
        
        // Throws error if container returns an error
        response.EnsureSuccessStatusCode();
        
        // Deserializes the model container's response into ImageGenerationProviderResponse DTO
        var result = JsonSerializer.Deserialize<ImageGenerationProviderResponse>(responseBody);

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

    /// <summary>
    /// Temporary placeholder for title generation
    /// </summary>
    /// <returns>
    /// In the current implementation it returns the prompt itself
    /// </returns>
    private Task<GenerateImageText> GenerateImageTextAsync(string prompt)
    {
        return Task.FromResult(new GenerateImageText
        {
            TitleDa = prompt,
            TitleEn = prompt
        });
    }
}