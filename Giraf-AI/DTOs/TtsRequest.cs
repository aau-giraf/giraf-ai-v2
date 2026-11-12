using System.ComponentModel.DataAnnotations;

namespace Giraf_AI.DTOs;

/// <summary>Input for speech synthesis. The client only supplies text.</summary>
public class TtsRequest
{
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public required string Text { get; init; }
}
