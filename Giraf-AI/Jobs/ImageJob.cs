using Giraf_AI.DTOs;

namespace Giraf_AI.Jobs;

public class ImageJob
{
    public Guid Id { get; } = Guid.NewGuid();
    public required ImageRequest Request { get; init; }
    public TaskCompletionSource<ImageResult> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public int QueueId { get; init; }
}
