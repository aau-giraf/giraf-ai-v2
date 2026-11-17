using System.Text.Json.Serialization;

namespace Giraf_AI.DTOs.Providers.TtsProviderDa;

public class TtsProviderResponseDa
{
    public byte[] Data { get; init; } = Array.Empty<byte>();
}