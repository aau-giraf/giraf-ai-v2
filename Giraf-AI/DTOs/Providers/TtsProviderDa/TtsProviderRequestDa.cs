using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Giraf_AI.DTOs.Providers.TtsProviderDa;

public class TtsProviderRequestDa // Plapre request class for TTS provider in Danish
{
    [Required]
    [JsonPropertyName("text")] 
    public required string Text { get; init; } = string.Empty;
    
    [JsonPropertyName("speaker")] 
    public required string Speaker { get; init; } = string.Empty;
    
    [JsonPropertyName("temperature")] 
    public double Temperature { get; init; }
    
    [JsonPropertyName("top_p")] 
    public double TopP { get; init; }
    
    [JsonPropertyName("top_k")] 
    public int TopK { get; init; }
    
    [JsonPropertyName("max_tokens")] 
    public int MaxTokens { get; init; }
}