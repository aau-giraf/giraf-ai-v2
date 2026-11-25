// Represents the image-generation status and result returned to API consumers.
using Giraf_AI.Models;
using System.Text.Json.Serialization;

namespace Giraf_AI.DTOs;

/// <summary>
/// Represents the result of an image generation request, including file paths and image text
/// </summary>
public class ImageResult
{
    public required string TitleDa { get; init; } = string.Empty;
    public required string TitleEn { get; init; } = string.Empty;
    public required string ImgId { get; init; } = string.Empty;
    public required string OriginApp { get; init; } = string.Empty;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? CitizenId { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ClassId { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? OrganizationId { get; init; }
    public required string ImageData { get; init; } = string.Empty;
}