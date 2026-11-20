// Carries the prompt and client identifiers needed to start an image-generation request.
using System.ComponentModel.DataAnnotations;

namespace Giraf_AI.DTOs;

/// <summary>
/// Contains the user‑provided prompt used to request image generation
/// </summary>
public class ImageRequest
{
    [Required(ErrorMessage = "Prompt is required")]
    [MaxLength(100, ErrorMessage = "Prompt cannot exceed 100 characters")]
    public required string Prompt { get; init; } = string.Empty;

    [Required(ErrorMessage = "ImgID is required")]
    [MaxLength(100, ErrorMessage = "ImgID cannot exceed 100 characters")]
    public required string ImgID { get; init; } = string.Empty;

    [Required(ErrorMessage = "MapID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "MapID must be a positive integer")]
    public required int MapID { get; init; }

    [Required(ErrorMessage = "ID type is required")]
    [RegularExpression("^(?i)(citizenid|classid|organizationid)$", 
        ErrorMessage = "IdType must be either 'citizenid', 'classid', or 'organizationid', case-insensitive")]
    public required string IdType { get; init; } = string.Empty;
    
    [Required(ErrorMessage = "Origin app is required")]
    [RegularExpression("^(?i)(vta|fp)$", 
        ErrorMessage = "OriginApp must be either 'vta' or 'fp', case-insensitive")]
    public required string OriginApp { get; init; } = string.Empty;
}