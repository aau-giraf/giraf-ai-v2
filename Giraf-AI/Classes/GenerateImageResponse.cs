namespace DefaultNamespace;

/// <summary>
/// Represents the result of an image generation request, including file paths and image text
/// </summary>
public class GenerateImageResponse
{
    public required List<string> Path { get; init; } = new();
    public GenerateImageText ImageText { get; init; } = new();
}