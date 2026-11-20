// Provides the bounded TTS queue used to decouple API requests from provider calls.
using System.Threading.Channels;
using Giraf_AI.Jobs;

namespace Giraf_AI.Services.Queue;

// <summary>
// Service responsible for managing a queue of text-to-speech (TTS) jobs, allowing
// for asynchronous processing of TTS requests in a thread-safe manner.
// </summary>

/// <summary>
/// Implements the text-to-speech queue.
/// </summary>
public class TtsQueueService : IQueueService<TtsJob>
{
    // Define a constant for the maximum capacity of the queue and create a bounded channel for TTS jobs
    private const int Capacity = 1000;
    // Create a bounded channel for TTS jobs with specified capacity and options
    private readonly Channel<TtsJob> _queue = Channel.CreateBounded<TtsJob>(
        new BoundedChannelOptions(Capacity)
        {
            // Set the behavior of the channel when it is full, allowing writers to wait until space is available
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });

    // Add item to queue
    public async ValueTask AddToQueue(TtsJob job, CancellationToken cancellationToken)
    {
        await _queue.Writer.WriteAsync(job, cancellationToken);
    }

    // Remove item from queue
    public async ValueTask<TtsJob> RemoveFromQueue(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
