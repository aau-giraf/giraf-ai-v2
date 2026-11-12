namespace Giraf_AI.Services.TTS;

/// <summary>
/// Processes one Plapre request at a time and completes the waiting HTTP request
/// with WAV bytes. Nothing is written to disk or a TTS database.
/// </summary>
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
                try
                {
                    // If CORE times out or disconnects, stop wasting Plapre capacity.
                    using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                        stoppingToken, job.RequestCancellationToken);

                    linkedCancellation.Token.ThrowIfCancellationRequested();

                    using IServiceScope scope = scopeFactory.CreateScope();
                    TtsGenerationService generator = scope.ServiceProvider
                        .GetRequiredService<TtsGenerationService>();

                    byte[] wav = await generator.GenerateAsync(
                        job.Text, linkedCancellation.Token);

                    job.Completion.TrySetResult(wav);
                }
                catch (OperationCanceledException) when (
                    stoppingToken.IsCancellationRequested ||
                    job.RequestCancellationToken.IsCancellationRequested)
                {
                    job.Completion.TrySetCanceled();

                    if (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "TTS job {JobId} failed.", job.Id);
                    job.Completion.TrySetException(exception);
                }
                finally
                {
                    queue.ReleaseSlot();
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal server shutdown.
        }
    }
}
