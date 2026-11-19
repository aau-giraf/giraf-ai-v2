// Stores generated descriptive text associated with an image.
namespace Giraf_AI.Models;
/// Contains the Danish and English titles devired from the prompt using the LLM

/// <summary>
/// Stores descriptive text associated with an image.
/// </summary>
public class ImageText
{
    public string TitleDa { get; init; } = string.Empty;
    public string TitleEn { get; init; } = string.Empty;
}