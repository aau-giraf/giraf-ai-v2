namespace Giraf_AI.DTOs;

/// <summary>Location of a successfully generated WAV file.</summary>
public class TtsResult
{
    public required Guid Id { get; init; }

    public required string AudioUrl { get; init; }
}
