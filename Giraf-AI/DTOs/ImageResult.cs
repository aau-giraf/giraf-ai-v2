using Giraf_AI.Models;
using System.Text.Json.Serialization;

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
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? CitizenID { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ClassID { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? OrganizationID { get; init; }
}