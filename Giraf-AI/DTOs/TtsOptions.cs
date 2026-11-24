// Holds optional synthesis settings that can be forwarded to the selected voice provider.
using System.ComponentModel.DataAnnotations;

namespace Giraf_AI.DTOs;

/// <summary>
/// Options for text-to-speech output
/// </summary>
public class TtsOptions
{
    [RegularExpression("^(da|en)$")]
    public string Language { get; init; } = "da";
}