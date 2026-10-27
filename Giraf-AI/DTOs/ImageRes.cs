using Giraf_AI.Models;

namespace Giraf_AI.DTOs;

/// <summary>
/// Represents the result of an image generation request, including file paths and image text
/// </summary>
public class ImageRes
{
    public required List<string> ImageData { get; init; } = new();
    public ImageText ImageText { get; init; } = new();
}