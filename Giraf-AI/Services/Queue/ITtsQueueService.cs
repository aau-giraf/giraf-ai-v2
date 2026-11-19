using Giraf_AI.Jobs;

namespace Giraf_AI.Services.Queue;

public interface ITtsQueueService
{
    ValueTask AddToQueue(TtsJob job, CancellationToken cancellationToken);
    ValueTask<TtsJob> RemoveFromQueue(CancellationToken cancellationToken);
}
