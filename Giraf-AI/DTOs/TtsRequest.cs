using System.ComponentModel.DataAnnotations;

namespace Giraf_AI.DTOs;

public class TtsRequest
{
    [Required(ErrorMessage = "Prompt is required")]
    [MaxLength(500, ErrorMessage = "Prompt cannot exceed 500 characters")]
    public required string Text { get; init; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "TtsID must be a positive integer")]
    public required int TtsID { get; init; }

    [Required]
    [RegularExpression("^(?i)(da|en)$")] // Ensures that the value is either "da" or "en", case-insensitive
    public required string Lang { get; init; } = string.Empty;
}