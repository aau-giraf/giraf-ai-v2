using Giraf_AI.Models;

namespace Giraf_AI.DTOs;

/// <summary>
/// Represents the result of an image generation request, including file paths and image text
/// </summary>
public class ImageResult
{
    public required string ImageData { get; init; } = string.Empty;
    public required string TitleDa { get; init; } = string.Empty;
	public required string TitleEn { get; init; } = string.Empty;
	public required string ImgID { get; init; } = string.Empty;
	public int CitizenID { get; init; }
	public int ClassID { get; init; }
	public int CrganizationID { get; init; }
}