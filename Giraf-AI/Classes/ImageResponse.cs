namespace Giraf_AI.Classes;

/// <summary>
/// Represents the result of an image generation request, including file paths and image text
/// </summary>
public class ImageResponse
{
    public required List<string> Path { get; init; } = new();
    public ImageText ImageText { get; init; } = new();
}