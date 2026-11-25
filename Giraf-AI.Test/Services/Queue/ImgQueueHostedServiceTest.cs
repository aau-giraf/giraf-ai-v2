using System.Net;
using Giraf_AI.Data;
using Giraf_AI.DTOs;
using Giraf_AI.Jobs;
using Giraf_AI.Models;
using Giraf_AI.Services.Queue;
using Giraf_AI.Test.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Giraf_AI.Test.Services.Queue;

public class ImgQueueHostedServiceTest
{
    public static ImgQueue CreateImgQueue()
    {
        return new ImgQueue
        {
            Prompt = "A Cat",
            ImageId = "1234",
            MapId = 1,
            IdType = IdType.citizen_id,
            OriginApp = OriginApp.fp,
            Received = DateTime.Now
        };
    }
    
    [Fact]
    public async Task Placeholder()
    {
        // Arrange
        /*var imageJob = CreateImageJob();
        var queue = new ImgQueueServiceMock();
        await queue.AddToQueue(imageJob, CancellationToken.None);

        var imageService = new ImageServiceMock();

        using var provider = ServiceFactory.CreateImgQueueHostedService(
            queue,
            httpClientFactory,
            imageService,
            options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        var hostedService = provider.GetRequiredService<ImgQueueHostedService>();*/

        // Act

        // Assert
    }

    [Fact]
    public async Task ProcessesRestoredJobAndDeletesQueueEntry()
    {
        var dbFactory = new DbContextFactory();
        var queueEntry = CreateImgQueue();

        await using (var db = dbFactory.CreateDbContext())
        {
            db.ImgGenQueues.Add(queueEntry);
            await db.SaveChangesAsync();
        }

        var queue = new ImgQueueServiceMock();
        var imageService = new ImageServiceMock();

        using var response = new HttpResponseMessage(HttpStatusCode.Accepted);

        var (provider, _) = ServiceFactory.CreateImgQueueHostedService(
            queue,
            imageService,
            dbFactory.Configure,
            response);

        using (provider)
        {
            var hostedService = provider.GetRequiredService<ImgQueueHostedService>();
            await hostedService.StartAsync(CancellationToken.None);

            var restoredJob = await queue
                .WaitForAddedJobAsync(CancellationToken.None)
                .AsTask().WaitAsync(TimeSpan.FromSeconds(5));

            var result = await restoredJob.Completion.Task.WaitAsync(TimeSpan.FromSeconds(5));

            await hostedService.StopAsync(CancellationToken.None);

            Assert.Equal(queueEntry.ImageId, result.ImgID);

            await using var verifyDb = dbFactory.CreateDbContext();
            Assert.Empty(await verifyDb.ImgGenQueues.ToListAsync());
        }
    }

    [Fact]
    public async Task DeliverResultUntilAccepted_RetriesIfNotAccepted()
    {
        var dbFactory = new DbContextFactory();
        var queueEntry = CreateImgQueue();

        await using (var db = dbFactory.CreateDbContext())
        {
            db.ImgGenQueues.Add(queueEntry);
            await db.SaveChangesAsync();
        }

        var queue = new ImgQueueServiceMock();
        var imageService = new ImageServiceMock();

        using var rejectedResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);
        using var acceptedResponse = new HttpResponseMessage(HttpStatusCode.Accepted);

        var (provider, handler) = ServiceFactory.CreateImgQueueHostedService(
            queue,
            imageService,
            dbFactory.Configure,
            rejectedResponse,
            acceptedResponse);

        using (provider)
        {
            var hostedService = provider.GetRequiredService<ImgQueueHostedService>();
            await hostedService.StartAsync(CancellationToken.None);

            try
            {
                var restoredJob = await queue
                    .WaitForAddedJobAsync(CancellationToken.None)
                    .AsTask()
                    .WaitAsync(TimeSpan.FromSeconds(5));

                await restoredJob.Completion.Task.WaitAsync(TimeSpan.FromSeconds(5));

                Assert.Equal(2, handler.RequestCount);
            }
            finally
            {
                await hostedService.StopAsync(CancellationToken.None);
            }
        }
    }
    
    [Fact]
    public async Task DeliverResultUntilAccepted_ContinuesRetryingIfNeverAccepted()
    {
        var dbFactory = new DbContextFactory();
        var queueEntry = CreateImgQueue();

        await using (var db = dbFactory.CreateDbContext())
        {
            db.ImgGenQueues.Add(queueEntry);
            await db.SaveChangesAsync();
        }

        var queue = new ImgQueueServiceMock();
        var imageService = new ImageServiceMock();

        using var rejectedResponse1 = new HttpResponseMessage(HttpStatusCode.BadRequest);
        using var rejectedResponse2 = new HttpResponseMessage(HttpStatusCode.BadRequest);
        using var rejectedResponse3 = new HttpResponseMessage(HttpStatusCode.BadRequest);
        using var rejectedResponse4 = new HttpResponseMessage(HttpStatusCode.BadRequest);
        
        var (provider, handler) = ServiceFactory.CreateImgQueueHostedService(
            queue,
            imageService,
            dbFactory.Configure,
            rejectedResponse1,
            rejectedResponse2,
            rejectedResponse3,
            rejectedResponse4);

        using (provider)
        {
            var hostedService = provider.GetRequiredService<ImgQueueHostedService>();
            await hostedService.StartAsync(CancellationToken.None);

            try
            {
                var restoredJob = await queue
                    .WaitForAddedJobAsync(CancellationToken.None)
                    .AsTask()
                    .WaitAsync(TimeSpan.FromSeconds(5));

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                try
                {
                    while (handler.RequestCount < 4)
                    {
                        await Task.Delay(10, timeout.Token);
                    }
                }
                catch (OperationCanceledException) when (timeout.IsCancellationRequested)
                {
                    Assert.Fail($"Expected 4 requests, but got {handler.RequestCount}");
                }

                Assert.False(restoredJob.Completion.Task.IsCompleted);
            }
            finally
            {
                await hostedService.StopAsync(CancellationToken.None);
            }
        }
    }
    
}