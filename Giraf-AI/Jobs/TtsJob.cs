using Giraf_AI.DTOs;

namespace Giraf_AI.Jobs;

public class TtsJob
{
    public Guid Id { get; } = Guid.NewGuid();
    public required TtsRequest Request { get; init; }
    public TaskCompletionSource<TtsResult> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public int TtsId { get; init; }
}
