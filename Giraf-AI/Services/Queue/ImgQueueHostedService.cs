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
    
    // <summary>
    // Restores any image jobs that were persisted in the database but not yet processed when the previous instance of the service stopped.
    // </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        
        // Re-enqueue database entries left behind by a previous process instance.
        await RestorePendingJobsAsync(stoppingToken);

        // ReadAsync waits for work and observes shutdown through the cancellation token.
        while (!stoppingToken.IsCancellationRequested)
        {
            ImageJob job;

            // try to dequeue the next job, but if the queue is empty, wait until a new job is added or the service is stopped.
            try
            {
                job = await imgQueueService.RemoveFromQueue(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            // Process the dequeued job and deliver the result to GIRAF CORE.
            try
            {
                using IServiceScope scope = scopeFactory.CreateScope();
                ImageService imageService = scope.ServiceProvider
                    .GetRequiredService<ImageService>();
                AppDbContext dbContext = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                // Process the image request and wait for the content check to complete.
                ImageResult result = await imageService.ProcessImageRequestAsync(
                    job.Request,
                    () => job.ContentCheck.TrySetResult());
                var girafCoreClient = httpClientFactory.CreateClient("GirafCore");
                
                // Deliver the result to GIRAF CORE and wait for a 202 Accepted response before removing the job from the database.
                await DeliverResultUntilAcceptedAsync(
                    girafCoreClient,
                    result,
                    job.Id,
                    stoppingToken);

                // Remove the completed job from the database after successful delivery to GIRAF CORE.
                int deletedRows = await dbContext.ImgGenQueues
                    .Where(entry => entry.ImageId == job.ImageId)
                    .ExecuteDeleteAsync(stoppingToken);
                if (deletedRows != 1)
                {
                    throw new InvalidOperationException(
                        $"Expected to delete queue entry {job.ImageId}, but deleted {deletedRows} rows.");
                }

            }
            // Handle content filter violations by setting the exception on the job's content check and removing the job from the database.
            catch (ContentFilterViolationException e)
            {
                job.ContentCheck.TrySetException(e);

                using IServiceScope scope = scopeFactory.CreateScope();
                AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                await dbContext.ImgGenQueues
                    .Where(entry => entry.ImageId == job.ImageId)
                    .ExecuteDeleteAsync(stoppingToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to process or deliver image job {JobId}.", job.Id);
                job.ContentCheck.TrySetException(e);
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
            .OrderBy(entry => entry.Received)
            .ThenBy(entry => entry.ImageId)
            .ToListAsync(cancellationToken);

        foreach (ImgQueue entry in pendingEntries)
        {
            var job = new ImageJob
            {
                ImageId = entry.ImageId,
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
                            $"Queue entry {entry.ImageId} has an unsupported ID type.")
                    },
                    OriginApp = entry.OriginApp switch
                    {
                        OriginApp.vta => "vta",
                        OriginApp.fp => "fp",
                        _ => throw new InvalidOperationException(
                            $"Queue entry {entry.ImageId} has an unsupported origin app.")
                    }
                }
            };

            await imgQueueService.AddToQueue(job, cancellationToken);
            logger.LogInformation(
                "Restored pending image job for queue entry {ImageId}.",
                entry.ImageId);
        }
    }
}
