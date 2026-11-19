// Defines the asynchronous contract for submitting and receiving TTS jobs.
using Giraf_AI.Jobs;

namespace Giraf_AI.Services.Queue;

// <summary>
// Interface for a service that manages a queue of text-to-speech (TTS) jobs
// </summary>

/// <summary>
/// Defines operations for the text-to-speech queue.
/// </summary>
public interface ITtsQueueService
{
    ValueTask AddToQueue(TtsJob job, CancellationToken cancellationToken);
    ValueTask<TtsJob> RemoveFromQueue(CancellationToken cancellationToken);
}
