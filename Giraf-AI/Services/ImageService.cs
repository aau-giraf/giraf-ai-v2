using System.Text;
using System.Text.Json;
using Giraf_AI.Classes;

namespace Giraf_AI.Services;

/// <summary>
/// Handles the entire image generation workflow: prepares the provider request,
/// calls the model container, and combines the image data with the temporary titles
/// </summary>
public class ImageService
{
    /// <summary>
    /// Initializes the service with an HttpClient for the image generation model container
    /// </summary>
    /// <param name="httpClientFactory">
    /// Factory is used instead of "new HttpClient()" because it allows
    /// for recycling of underlying connection handlers to avoid socket exhaustion
    /// </param>
    public ImageService(IHttpClientFactory httpClientFactory)
    {
        _imageGenerationClient = httpClientFactory.CreateClient("ImageGeneration");
    }
    
    private readonly HttpClient _imageGenerationClient;

    /// <summary>
    /// Generates an image based on the user request
    /// Combines model output (base64 image data) with the temporary title data
    /// </summary>
    /// <param name="request">Includes the prompt from the user</param>
    /// <returns>
    /// Returns the image data (base64) and temporary titles
    /// </returns>
    public async Task<ImageResponse> GenerateImageAsync(ImageRequest request)
    {
        var imageGenerateRequest = new ImageProviderRequest
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

        return new ImageResponse
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
    private async Task<List<string>> GenerateImageBase64Async(ImageProviderRequest request)
    {
        // Serializes the request into JSON for the model container
        var body = JsonSerializer.Serialize(request);
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        // Send request to the model container endpoint
        using var response = await this._imageGenerationClient.PostAsync("v1/images/generations", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        // Throws error if container returns an error
        response.EnsureSuccessStatusCode();

        // Deserializes the model container's response into ImageProviderResponse DTO
        var result = JsonSerializer.Deserialize<ImageProviderResponse>(responseBody);

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
    private static Task<ImageText> GenerateImageTextAsync(string prompt)
    {
        return Task.FromResult(new ImageText
        {
            TitleDa = prompt,
            TitleEn = prompt
        });
    }
}
