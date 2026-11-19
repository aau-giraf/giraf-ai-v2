// Drains TTS jobs in the background and dispatches them to the language-specific provider.
using System.Net;
using System.Net.Http.Json;
using Giraf_AI.Data;
using Giraf_AI.DTOs;
using Giraf_AI.Jobs;
using Giraf_AI.Models;
using Giraf_AI.Services;
using Microsoft.EntityFrameworkCore;

namespace Giraf_AI.Services.Queue;

/// <summary>
/// Processes queued text-to-speech jobs and delivers completed results to GIRAF CORE.
/// Jobs remain persisted until the callback has been acknowledged successfully.
/// </summary>
public sealed class TtsQueueHostedService(
    ITtsQueueService queueService,
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<TtsQueueHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Re-enqueue database entries left behind by a previous process instance.
        await RestorePendingJobsAsync(stoppingToken);
        
        // ReadAsync waits for work and observes shutdown through the cancellation token.
        while (!stoppingToken.IsCancellationRequested)
        {
            // Declare the job outside the dequeue try block so it is available to
            // the processing block after the queue has supplied the next item.
            TtsJob job;

            try
            {
                // Remove the next waiting job. This call pauses the worker when the
                // queue is empty instead of repeatedly polling the queue.
                job = await queueService.RemoveFromQueue(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Cancellation is the expected way for the worker to stop while
                // waiting for another job, so leave the loop without logging an error.
                break;
            }
            
            try
            {
                // TtsService and AppDbContext are scoped, so each job gets a fresh scope.
                using IServiceScope scope = scopeFactory.CreateScope();

                // Resolve the service that converts the request text into audio.
                TtsService ttsService = scope.ServiceProvider.GetRequiredService<TtsService>();

                // Resolve the database context used to remove the completed job.
                AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Generate the audio before notifying GIRAF CORE that the job is complete.
                TtsResult result = await ttsService.ProcessTtsRequestAsync(job.Request);

                // Use the named client so the configured GIRAF CORE base address and
                // HTTP settings are applied consistently.
                var girafCoreClient = httpClientFactory.CreateClient("GirafCore");
                
                // Keep the database entry until CORE confirms receipt with 202 Accepted.
                using var response = await girafCoreClient.PostAsJsonAsync(
                    configuration["Tts:CoreCallbackPath"] ?? "tts/", result, stoppingToken);

                // Convert HTTP error responses such as 400 or 500 into exceptions
                // handled by the job failure path below.
                response.EnsureSuccessStatusCode();

                // A successful HTTP status alone is not enough: this callback
                // contract specifically requires 202 Accepted.
                if (response.StatusCode != HttpStatusCode.Accepted)
                {
                    throw new HttpRequestException(
                        $"GIRAF CORE returned {(int)response.StatusCode}; expected 202 Accepted.");
                }
                
                // Deleting the entry acknowledges completion and prevents replay after restart.
                int deletedRows = await dbContext.TtsQueues
                    // Match by the persisted TTS identifier rather than the in-memory
                    // job object, which is not an entity tracked by this context.
                    .Where(entry => entry.TtsId == job.TtsId)
                    // Execute the delete directly in the database without loading the
                    // entity or relying on change tracking.
                    .ExecuteDeleteAsync(stoppingToken);

                // Exactly one persisted entry should represent this job. Any other
                // result indicates a persistence or duplicate-data problem.
                if (deletedRows != 1)
                {
                    throw new InvalidOperationException(
                        $"Expected to delete TTS entry {job.TtsId}, but deleted {deletedRows} rows.");
                }

                // Complete the caller's task only after both delivery and cleanup
                // have succeeded.
                job.Completion.TrySetResult(result);
            }
            catch (Exception e)
            {
                // Report the failure and complete the caller's task exceptionally,
                // allowing the request that created the job to observe the error.
                logger.LogError(e, "Failed to process or deliver TTS job {JobId}.", job.Id);
                job.Completion.TrySetException(e);
            }
        }
    }

    private async Task RestorePendingJobsAsync(CancellationToken cancellationToken)
    {
        // Jobs are persisted before they enter the in-memory channel, so restore any
        // entries that were still pending when the previous instance stopped.
        using IServiceScope scope = scopeFactory.CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Read all persisted jobs without tracking because these records are only
        // used to reconstruct queue messages; they are not edited here.
        List<TtsQueue> pendingEntries = await dbContext.TtsQueues
            .AsNoTracking()
            // Restore in TTS ID order to keep recovery deterministic.
            .OrderBy(entry => entry.TtsId)
            .ToListAsync(cancellationToken);

        foreach (TtsQueue entry in pendingEntries)
        {
            // Rebuild the in-memory job from the fields stored in the database.
            var job = new TtsJob
            {
                TtsId = entry.TtsId,
                Request = new TtsRequest
                {
                    // Restore the original text so it can be synthesized again.
                    Text = entry.Prompt,
                    // The request and queue record must refer to the same TTS job.
                    TtsID = entry.TtsId,
                    // Convert the database enum into the language code expected by
                    // the TTS provider.
                    Lang = entry.Lang switch
                    {
                        LangType.da => "da",
                        LangType.en => "en",
                        // Do not silently restore an unsupported value. Failing
                        // startup makes invalid persisted data visible immediately.
                        _ => throw new InvalidOperationException(
                            $"TTS entry {entry.TtsId} has an unsupported language.")
                    }
                }
            };

            // Put the reconstructed job back into the channel for normal processing.
            await queueService.AddToQueue(job, cancellationToken);

            // Record each restored job to make startup recovery observable.
            logger.LogInformation(
                "Restored pending TTS job for TTS entry {TtsId}.",
                entry.TtsId);
        }
    }
}
