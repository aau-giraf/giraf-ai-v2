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

            await restoredJob.ContentCheck.Task.WaitAsync(TimeSpan.FromSeconds(5));

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            while (true)
            {
                await using var pollDb = dbFactory.CreateDbContext();
                if (!await pollDb.ImgGenQueues.AnyAsync())
                {
                    break;
                }

                await Task.Delay(10, timeout.Token);
            }

            await hostedService.StopAsync(CancellationToken.None);

            Assert.Equal(queueEntry.ImageId, restoredJob.Request.ImgID);

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

                await restoredJob.ContentCheck.Task.WaitAsync(TimeSpan.FromSeconds(5));

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                try
                {
                    while (handler.RequestCount < 2)
                    {
                        await Task.Delay(10, timeout.Token);
                    }
                }
                catch (OperationCanceledException) when (timeout.IsCancellationRequested)
                {
                    Assert.Fail($"Expected 2 requests, but got {handler.RequestCount}");
                }
                
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
        
        var (provider, handler) = ServiceFactory.CreateImgQueueHostedService(
            queue,
            imageService,
            dbFactory.Configure,
            () => new HttpResponseMessage(HttpStatusCode.BadRequest));

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
                const int expectedAttempts = 5;
                
                try
                {
                    while (handler.RequestCount < expectedAttempts)
                    {
                        await Task.Delay(10, timeout.Token);
                    }
                }
                catch (OperationCanceledException) when (timeout.IsCancellationRequested)
                {
                    Assert.Fail($"Expected {expectedAttempts} requests, but got {handler.RequestCount}");
                }
                
                Assert.True(handler.RequestCount >= expectedAttempts,
                    $"Expected at least {expectedAttempts} requests, but got {handler.RequestCount}");
            }
            finally
            {
                await hostedService.StopAsync(CancellationToken.None);
            }
        }
    }
}