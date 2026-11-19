// Defines the Danish TTS provider response payload.
using System.Text.Json.Serialization;

namespace Giraf_AI.DTOs.Providers.TtsProviderDa;

/// <summary>
/// Contains the Danish speech-provider response.
/// </summary>

public class TtsProviderResponseDa
{
    public byte[] Data { get; init; } = Array.Empty<byte>();
}