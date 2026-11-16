namespace Giraf_AI.DTOs;
using System.Text.Json.Serialization;

public class TtsResult
{
    [JsonPropertyName("audio")]
    public string Audio { get; init; } = string.Empty;
}