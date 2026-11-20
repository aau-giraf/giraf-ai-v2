// Contains the queued image-generation input and completion metadata.
using Giraf_AI.DTOs;

namespace Giraf_AI.Jobs;

/// <summary>
/// Represents work queued for image generation.
/// </summary>

public class ImageJob
{
    public Guid Id { get; } = Guid.NewGuid();
    public required ImageRequest Request { get; init; }
    public TaskCompletionSource<ImageResult> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public int QueueId { get; init; }
}
