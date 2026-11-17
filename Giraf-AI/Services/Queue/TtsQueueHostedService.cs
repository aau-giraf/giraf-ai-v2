using System.Net;
using System.Net.Http.Json;
using Giraf_AI.Data;
using Giraf_AI.DTOs;
using Giraf_AI.Jobs;
using Giraf_AI.Models;
using Giraf_AI.Services;
using Microsoft.EntityFrameworkCore;

namespace Giraf_AI.Services.Queue;

public sealed class TtsQueueHostedService(
    ITtsQueueService queueService,
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<TtsQueueHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RestorePendingJobsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            TtsJob job;

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
                TtsService ttsService = scope.ServiceProvider.GetRequiredService<TtsService>();
                AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                TtsResult result = await ttsService.ProcessTtsRequestAsync(job.Request);
                var girafCoreClient = httpClientFactory.CreateClient("GirafCore");
                // TODO: Confirm CORE callback path and JSON contract with the CORE team.
                using var response = await girafCoreClient.PostAsJsonAsync(
                    configuration["Tts:CoreCallbackPath"] ?? "tts/", result, stoppingToken);
                response.EnsureSuccessStatusCode();
                if (response.StatusCode != HttpStatusCode.Accepted)
                {
                    throw new HttpRequestException(
                        $"GIRAF CORE returned {(int)response.StatusCode}; expected 202 Accepted.");
                }

                int deletedRows = await dbContext.TtsQueues
                    .Where(entry => entry.QueueId == job.QueueId)
                    .ExecuteDeleteAsync(stoppingToken);
                if (deletedRows != 1)
                {
                    throw new InvalidOperationException(
                        $"Expected to delete queue entry {job.QueueId}, but deleted {deletedRows} rows.");
                }

                job.Completion.TrySetResult(result);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to process or deliver TTS job {JobId}.", job.Id);
                job.Completion.TrySetException(e);
            }
        }
    }

    private async Task RestorePendingJobsAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        List<TtsQueue> pendingEntries = await dbContext.TtsQueues
            .AsNoTracking()
            .OrderBy(entry => entry.QueueId)
            .ToListAsync(cancellationToken);

        foreach (TtsQueue entry in pendingEntries)
        {
            var job = new TtsJob
            {
                QueueId = entry.QueueId,
                Request = new TtsRequest
                {
                    Text = entry.Prompt,
                    TtsID = entry.TtsId,
                    Lang = entry.Lang switch
                    {
                        LangType.da => "da",
                        LangType.en => "en",
                        _ => throw new InvalidOperationException(
                            $"Queue entry {entry.QueueId} has an unsupported language.")
                    }
                }
            };

            await queueService.AddToQueue(job, cancellationToken);
            logger.LogInformation(
                "Restored pending TTS job for queue entry {QueueId}.",
                entry.QueueId);
        }
    }
}
