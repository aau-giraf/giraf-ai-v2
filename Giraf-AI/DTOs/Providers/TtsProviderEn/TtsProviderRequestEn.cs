using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Giraf_AI.DTOs.Providers.TtsProviderEn;

public class TtsProviderRequestEn // Kokoro request class for TTS provider in English
{
    [Required]
    [JsonPropertyName("input")] 
    public required string Input { get; init; } = string.Empty;
    
    [JsonPropertyName("accent")] 
    public required string Accent { get; init; } = string.Empty;
    
    [JsonPropertyName("voice")] 
    public required string Voice { get; init; } = string.Empty;
    
    [JsonPropertyName("response_format")] 
    public required string ResponseFormat { get; init; } = string.Empty;
}