using System.Text.Json.Serialization;

namespace Giraf_AI.DTOs;

public class TtsResult
{
    public required string Audio { get; init; }
    public required string Text { get; init; }
    public required int TtsID { get; init; }
}
