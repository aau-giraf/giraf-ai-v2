using System.Text.Json.Serialization;

namespace DefaultNamespace;

/// <summary>
/// Contains the image data returned by the model container for one or more images
/// </summary>
internal class ImageGenerationProviderResponse
{
    [JsonPropertyName("data")] 
    public List<GenerateImageFileData> Data { get; init; } = [];
}

/// <summary>
/// Contains a single generated image encoded as base64 JSON
/// </summary>
internal class GenerateImageFileData
{
    [JsonPropertyName("b64_json")]
    public string B64Json { get; init; } = string.Empty;
}