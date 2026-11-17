using System.Text.Json.Serialization;

namespace Giraf_AI.DTOs;

public class TtsResult
{
    [JsonPropertyName("audio")]
    public required string Audio { get; init; }
    public required int TtsID { get; init; }
    public required string Origin { get; init; }
}
