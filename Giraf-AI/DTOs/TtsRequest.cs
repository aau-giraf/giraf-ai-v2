using System.ComponentModel.DataAnnotations;

namespace Giraf_AI.DTOs;

public class TtsRequest
{
    [Required(ErrorMessage = "Prompt is required")]
    [MaxLength(500, ErrorMessage = "Prompt cannot exceed 500 characters")]
    public required string Prompt { get; init; } = string.Empty;
    
    [Required(ErrorMessage = "TtsID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "TtsID must be a positive integer")]
    public required int TtsID { get; init; } = 0;
    
    [Required(ErrorMessage = "Language type is required")]
    [AllowedValues("Da", "En",
        ErrorMessage = "Language must be one of: Da, En")]
    public TtsOptions Language { get; init; } = new();
}