namespace Giraf_AI.DTOs;

/// <summary>Current state of a queued speech-generation request.</summary>
public sealed record TtsJobStatus
{
    public required Guid Id { get; init; }
    public required string Status { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset? StartedAtUtc { get; init; }
    public DateTimeOffset? CompletedAtUtc { get; init; }
    public string? AudioUrl { get; init; }
    public string? Error { get; init; }
}
