// Defines the English TTS provider response payload.

using System.Text.Json.Serialization;

namespace Giraf_AI.DTOs.Providers.TtsProviderEn;

/// <summary>
/// Contains the English speech-provider response. Not used 
/// </summary>

public class TtsProviderResponseEn
{
    [JsonPropertyName("response")] public required byte[] LlmResponse { get; init; } = [];
}