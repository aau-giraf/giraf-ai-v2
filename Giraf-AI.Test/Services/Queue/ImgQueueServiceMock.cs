using Giraf_AI.Jobs;
using Giraf_AI.Services.Queue;

namespace Giraf_AI.Test.Services.Queue;

public class ImgQueueServiceMock : IQueueService<ImageJob>
{
    private readonly Queue<ImageJob> _queue = new();

    public ValueTask AddToQueue(ImageJob job, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _queue.Enqueue(job);

        return ValueTask.CompletedTask;
    }

    public ValueTask<ImageJob> RemoveFromQueue(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(_queue.Dequeue());
    }
}