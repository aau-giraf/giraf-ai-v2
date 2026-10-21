using System.Text.Json.Serialization;

namespace Giraf_AI.Classes.ImageProvider;

/// <summary>
/// Contains the image data returned by the model container for one or more images.
/// </summary>
public class ImageProviderRes
{
    [JsonPropertyName("data")]
    public List<ImageProviderData> Data { get; init; } = [];
}
