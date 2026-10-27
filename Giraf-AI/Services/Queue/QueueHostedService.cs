using Giraf_AI.DTOs;
using Giraf_AI.Jobs;

namespace Giraf_AI.Services.Queue;

public sealed class QueueHostedService(
    IQueueService queueService,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            ImageJob job;

            try
            {
                job = await queueService.RemoveFromQueue(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                using IServiceScope scope = scopeFactory.CreateScope();
                ImageService imageService = scope.ServiceProvider
                    .GetRequiredService<ImageService>();

                ImageRes result = await imageService.ProcessImageRequestAsync(job.Request);
                job.Completion.TrySetResult(result);
            }
            catch (Exception e)
            {
                job.Completion.TrySetException(e);
            }
        }
    }
}
