namespace Giraf_AI.Services.TTS;

/// <summary>Processes queued TTS jobs sequentially, independently of HTTP requests.</summary>
public sealed class TtsQueueWorker(
    TtsQueueService queue,
    IServiceScopeFactory scopeFactory,
    ILogger<TtsQueueWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (TtsQueueItem job in queue.ReadAllAsync(stoppingToken))
            {
                queue.MarkProcessing(job.Id);

                try
                {
                    using IServiceScope scope = scopeFactory.CreateScope();
                    TtsGenerationService generator = scope.ServiceProvider
                        .GetRequiredService<TtsGenerationService>();

                    var result = await generator.GenerateAsync(
                        job.Text,
                        job.Id,
                        stoppingToken);

                    queue.MarkCompleted(job.Id, result.AudioUrl);
                    job.Completion?.TrySetResult(result);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    queue.MarkFailed(job.Id, "The server stopped during speech generation.");
                    job.Completion?.TrySetCanceled(stoppingToken);
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "TTS job {JobId} failed.", job.Id);
                    queue.MarkFailed(job.Id, "Speech generation failed.");
                    job.Completion?.TrySetException(exception);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown while waiting for another job.
        }
    }
}
