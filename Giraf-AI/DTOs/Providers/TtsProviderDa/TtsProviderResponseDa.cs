// Defines the Danish TTS provider response payload.
using System.Text.Json.Serialization;

namespace Giraf_AI.DTOs.Providers.TtsProviderDa;

/// <summary>
/// Contains the Danish speech-provider response. Not used
/// </summary>

public class TtsProviderResponseDa
{
    [JsonPropertyName("response")] public required byte[] LlmResponse { get; init; } = [];
}