using Giraf_AI.Jobs;

namespace Giraf_AI.Services.Queue;

public interface IQueueService
{
    ValueTask AddToQueue(ImageJob job, CancellationToken cancellationToken);
    ValueTask<ImageJob> RemoveFromQueue(CancellationToken cancellationToken);
}