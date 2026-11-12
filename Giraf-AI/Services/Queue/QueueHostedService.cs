using System.Net;
using System.Net.Http.Json;
using Giraf_AI.Data;
using Giraf_AI.DTOs;
using Giraf_AI.Jobs;
using Giraf_AI.Models;
using Microsoft.EntityFrameworkCore;

namespace Giraf_AI.Services.Queue;

public sealed class QueueHostedService(
    IQueueService queueService,
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    ILogger<QueueHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RestorePendingJobsAsync(stoppingToken);

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
                AppDbContext dbContext = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                ImageResult result = await imageService.ProcessImageRequestAsync(job.Request);
                var girafCoreClient = httpClientFactory.CreateClient("GirafCore");
                using var response = await girafCoreClient.PostAsJsonAsync("images/", result, stoppingToken);
                response.EnsureSuccessStatusCode();
                if (response.StatusCode != HttpStatusCode.Accepted)
                {
                    throw new HttpRequestException(
                        $"GIRAF CORE returned {(int)response.StatusCode}; expected 202 Accepted.");
                }

                int deletedRows = await dbContext.ImgGenQueues
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
                logger.LogError(e, "Failed to process or deliver image job {JobId}.", job.Id);
                job.Completion.TrySetException(e);
            }
        }
    }

    private async Task RestorePendingJobsAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        List<ImgGenQueue> pendingEntries = await dbContext.ImgGenQueues
            .AsNoTracking()
            .OrderBy(entry => entry.QueueId)
            .ToListAsync(cancellationToken);

        foreach (ImgGenQueue entry in pendingEntries)
        {
            var job = new ImageJob
            {
                QueueId = entry.QueueId,
                Request = new ImageRequest
                {
                    Prompt = entry.Prompt,
                    ImgID = entry.ImageId,
                    MapID = entry.MapId,
                    IdType = entry.IdType switch
                    {
                        IdType.citizen_id => "CitizenID",
                        IdType.class_id => "ClassID",
                        IdType.organization_id => "OrganizationID",
                        _ => throw new InvalidOperationException(
                            $"Queue entry {entry.QueueId} has an unsupported ID type.")
                    }
                }
            };

            await queueService.AddToQueue(job, cancellationToken);
            logger.LogInformation(
                "Restored pending image job for queue entry {QueueId}.",
                entry.QueueId);
        }
    }
}
