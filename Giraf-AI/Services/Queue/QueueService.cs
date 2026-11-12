using System.Threading.Channels;
using Giraf_AI.Jobs;

namespace Giraf_AI.Services.Queue;

public class QueueService : IQueueService
{
    private const int Capacity = 1000;
    private readonly Channel<ImageJob> _queue = Channel.CreateBounded<ImageJob>(
        new BoundedChannelOptions(Capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
    
    

	// Add item to queue
    public async ValueTask AddToQueue(ImageJob job, CancellationToken cancellationToken)
    {
        await _queue.Writer.WriteAsync(job, cancellationToken);
    }

	// Remove item from queue
    public async ValueTask<ImageJob> RemoveFromQueue(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}