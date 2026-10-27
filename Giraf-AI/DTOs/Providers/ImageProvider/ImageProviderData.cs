using System.Text.Json.Serialization;

namespace Giraf_AI.DTOs.Providers.ImageProvider;

/// <summary>
/// Contains a single generated image encoded as base64 JSON.
/// </summary>
public class ImageProviderData
{
    [JsonPropertyName("b64_json")]
    public string B64Json { get; init; } = string.Empty;
}
