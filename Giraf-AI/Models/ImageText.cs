namespace Giraf_AI.Models;

/// Contains the Danish and English titles devired from the prompt using the LLM
public class ImageText
{
    public string TitleDa { get; init; } = string.Empty;
    public string TitleEn { get; init; } = string.Empty;
}