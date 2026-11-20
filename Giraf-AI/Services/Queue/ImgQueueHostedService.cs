// Drains image jobs in the background and invokes the image provider safely.
using System.Net;
using System.Net.Http.Json;
using Giraf_AI.Data;
using Giraf_AI.DTOs;
using Giraf_AI.Jobs;
using Giraf_AI.Models;
using Microsoft.EntityFrameworkCore;

namespace Giraf_AI.Services.Queue;

/// <summary>
/// Processes image-generation jobs in the background.
/// </summary>

public sealed class ImgQueueHostedService(
    IQueueService<ImageJob> imgQueueService,
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    ILogger<ImgQueueHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RestorePendingJobsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            ImageJob job;

            try
            {
                job = await imgQueueService.RemoveFromQueue(stoppingToken);
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
                await DeliverResultUntilAcceptedAsync(
                    girafCoreClient,
                    result,
                    job.Id,
                    stoppingToken);

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
            catch (ContentFilterViolationException e)
            {
                using IServiceScope scope = scopeFactory.CreateScope();
                AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                await dbContext.ImgGenQueues
                    .Where(entry => entry.QueueId == job.QueueId)
                    .ExecuteDeleteAsync(stoppingToken);

                job.Completion.TrySetException(e);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to process or deliver image job {JobId}.", job.Id);
                job.Completion.TrySetException(e);
            }
        }
    }

    private async Task DeliverResultUntilAcceptedAsync(
        HttpClient client,
        ImageResult result,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            try
            {
                using HttpResponseMessage response = await client.PostAsJsonAsync(
                    "images/", result, cancellationToken);

                if (response.StatusCode == HttpStatusCode.Accepted)
                {
                    return;
                }

                logger.LogWarning(
                    "Image callback for job {JobId} returned status {StatusCode}; retrying.",
                    jobId,
                    (int)response.StatusCode);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    "Image callback for job {JobId} timed out; retrying.",
                    jobId);
            }
            catch (HttpRequestException e)
            {
                logger.LogWarning(
                    e,
                    "Image callback for job {JobId} failed; retrying.",
                    jobId);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
    }

    private async Task RestorePendingJobsAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        List<ImgQueue> pendingEntries = await dbContext.ImgGenQueues
            .AsNoTracking()
            .OrderBy(entry => entry.QueueId)
            .ToListAsync(cancellationToken);

        foreach (ImgQueue entry in pendingEntries)
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
                    },
                    OriginApp = entry.OriginApp switch
                    {
                        OriginApp.vta => "vta",
                        OriginApp.fp => "fp",
                        _ => throw new InvalidOperationException(
                            $"Queue entry {entry.QueueId} has an unsupported origin app.")
                    }
                }
            };

            await imgQueueService.AddToQueue(job, cancellationToken);
            logger.LogInformation(
                "Restored pending image job for queue entry {QueueId}.",
                entry.QueueId);
        }
    }
}
