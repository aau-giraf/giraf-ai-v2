using System.ComponentModel.DataAnnotations;

namespace Giraf_AI.Classes;

/// <summary>
/// Contains the user‑provided prompt used to request image generation
/// </summary>
public class ImageRequest
{
    [Required(ErrorMessage = "Prompt is required")]
    [MaxLength(100, ErrorMessage = "Prompt cannot exceed 100 characters")]
    public required string Prompt { get; init; } = string.Empty;
}