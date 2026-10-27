using Giraf_AI.DTOs;

namespace Giraf_AI.Jobs;

public class ImageJob
{
    public Guid Id { get; } = Guid.NewGuid();
    public required ImageReq Request { get; init; }
    public TaskCompletionSource<ImageRes> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}
