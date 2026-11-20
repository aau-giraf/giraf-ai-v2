// Carries text, language, and client identifiers for a text-to-speech request.
using System.ComponentModel.DataAnnotations;
using Giraf_AI.Models;

namespace Giraf_AI.DTOs;

/// <summary>
/// Describes a text-to-speech request received by the API.
/// </summary>

public class TtsRequest
{
    [Required(ErrorMessage = "Prompt is required")]
    [MaxLength(500, ErrorMessage = "Prompt cannot exceed 500 characters")]
    public required string Text { get; init; } = string.Empty;

    [Required(ErrorMessage = "TtsID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "TtsID must be a positive integer")]
    public required int TtsID { get; init; }
    
    [Required(ErrorMessage = "OriginApp is required")]
    public required OriginApp? OriginApp { get; init; }

    [RegularExpression("^(?i)(da|en)$", 
        ErrorMessage = "Lang must be either 'da' or 'en', case-insensitive")] // Ensures that the value is either "da" or "en", case-insensitive
    public required string? Lang { get; init; } = string.Empty;
}