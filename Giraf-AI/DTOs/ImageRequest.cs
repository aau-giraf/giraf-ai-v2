using System.ComponentModel.DataAnnotations;

namespace Giraf_AI.DTOs;

/// <summary>
/// Contains the user‑provided prompt used to request image generation
/// </summary>
public class ImageRequest : IValidatableObject
{
    private static readonly string[] AllowedIdTypes =
    [
        "CitizenID",
        "ClassID",
        "OrganizationID"
    ];

    [Required(ErrorMessage = "Prompt is required")]
    [MaxLength(100, ErrorMessage = "Prompt cannot exceed 100 characters")]
    public required string Prompt { get; init; } = string.Empty;
    [Required(ErrorMessage = "ImgID is required")]
    public required string ImgID { get; init; } = string.Empty;
    [Required(ErrorMessage = "MapID is required")]
    public required int MapID { get; init; }
    [Required(ErrorMessage = "ID type is required")]
    public required string IdType { get; init; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!AllowedIdTypes.Contains(IdType, StringComparer.Ordinal))
        {
            yield return new ValidationResult(
                $"IdType must be one of: {string.Join(", ", AllowedIdTypes)}.",
                [nameof(IdType)]);
        }
    }
}