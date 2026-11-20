// Defines the asynchronous contract for submitting and receiving TTS jobs.
using Giraf_AI.Jobs;

namespace Giraf_AI.Services.Queue;

// <summary>
// Interface for a service that manages a queue of TTS and Image jobs
// </summary>

/// <summary>
/// Defines operations for the TTS and Image queue.
/// </summary>
public interface IQueueService<T>
{
    ValueTask AddToQueue(T job, CancellationToken cancellationToken);
    ValueTask<T> RemoveFromQueue(CancellationToken cancellationToken);
}