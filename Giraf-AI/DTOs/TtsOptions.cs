using System.ComponentModel.DataAnnotations;

namespace Giraf_AI.DTOs;

/// <summary>
/// Preferences for text-to-speech output
/// </summary>
public class TtsOptions
{
    [RegularExpression("^(da|en)$")]
    public string Language { get; init; } = "da";
    [RegularExpression("^(british|american)$")]
    public string? Accent { get; init; } = null; // default language is danish, therefore no default accent.
    [RegularExpression("[fFmM]")]
    public char Gender { get; init; } = 'f';
}