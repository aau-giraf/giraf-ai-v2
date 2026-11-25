using System.Threading.Channels;
using Giraf_AI.Jobs;
using Giraf_AI.Services.Queue;

namespace Giraf_AI.Test.Services.Queue;

public class ImgQueueServiceMock : IQueueService<ImageJob>
{
    /*private readonly Queue<ImageJob> _queue = new();
    //private readonly TaskCompletionSource<ImageJob> _ended = new(TaskCreationOptions.RunContinuationsAsynchronously);

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
    }*/
    private readonly Channel<ImageJob> _queue = Channel.CreateUnbounded<ImageJob>();
    private readonly Channel<ImageJob> _addedJobs = Channel.CreateUnbounded<ImageJob>();

    public async ValueTask AddToQueue(ImageJob job, CancellationToken cancellationToken)
    {
        await _addedJobs.Writer.WriteAsync(job, cancellationToken);
        await _queue.Writer.WriteAsync(job, cancellationToken);
    }

    public ValueTask<ImageJob> RemoveFromQueue(CancellationToken cancellationToken) =>
        _queue.Reader.ReadAsync(cancellationToken);

    public ValueTask<ImageJob> WaitForAddedJobAsync(CancellationToken cancellationToken) =>
        _addedJobs.Reader.ReadAsync(cancellationToken);
}