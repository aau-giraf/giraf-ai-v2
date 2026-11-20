// Represents the status and location of generated speech returned by the API.
using System.Text.Json.Serialization;

namespace Giraf_AI.DTOs;

/// <summary>
/// Describes the text-to-speech result exposed by the API.
/// </summary>

public class TtsResult
{
    public required string Audio { get; init; }
    public required string Text { get; init; }
    public required int TtsID { get; init; }
}
