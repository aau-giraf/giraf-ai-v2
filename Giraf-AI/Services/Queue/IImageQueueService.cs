// Defines the asynchronous contract for submitting and receiving image jobs.
using Giraf_AI.Jobs;

namespace Giraf_AI.Services.Queue;

// <summary>
// Interface for a service that manages a queue of image generation jobs
// </summary>

/// <summary>
/// Defines operations for the image-generation queue.
/// </summary>
public interface IImageQueueService
{
    ValueTask AddToQueue(ImageJob job, CancellationToken cancellationToken);
    ValueTask<ImageJob> RemoveFromQueue(CancellationToken cancellationToken);
}