using Giraf_AI.DTOs;

namespace Giraf_AI.Services;

public interface IImageGenerationQueueRepository
{
    Task<int> EnqueueAsync(ImageRequest request, CancellationToken cancellationToken);
}
