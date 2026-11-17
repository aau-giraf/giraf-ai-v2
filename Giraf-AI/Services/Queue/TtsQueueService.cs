using System.Threading.Channels;
using Giraf_AI.Jobs;

namespace Giraf_AI.Services.Queue;

public class TtsQueueService : ITtsQueueService
{
    private const int Capacity = 1000;
    private readonly Channel<TtsJob> _queue = Channel.CreateBounded<TtsJob>(
        new BoundedChannelOptions(Capacity)
        {
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
