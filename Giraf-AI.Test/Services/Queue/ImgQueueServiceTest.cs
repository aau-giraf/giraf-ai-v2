using Giraf_AI.DTOs;
using Giraf_AI.Jobs;
using Giraf_AI.Services.Queue;

namespace Giraf_AI.Test.Services.Queue;

public class ImgQueueServiceTest
{
    private static ImageJob CreateImageJob()
    {
        return new ImageJob
        {
            QueueId = 123,
            Request = new ImageRequest
            {
                Prompt = "A Cat",
                ImgID = Guid.NewGuid().ToString(),
                MapID = 123,
                IdType = "CitizenID",
                OriginApp = "fp"
            }
        };
    }
    
    [Fact]
    public async Task QueueService_EnqueuesAndDequeuesSameObject()
    {
        var service = new ImgQueueService();
        var imageJob = CreateImageJob();

        await service.AddToQueue(imageJob, CancellationToken.None);

        var result = await service.RemoveFromQueue(CancellationToken.None);

        Assert.Same(imageJob, result);
    }
    /*[Fact]
    public Task QueueService_DequeueAsync_ReturnsQueuedItem()
    {
        
    }*/
    [Fact]
    public async Task QueueService_DequeueAsync_ReturnsItemsInOrder()
    {
        // Arrange
        var service = new ImgQueueService();
        var firstImageJob = CreateImageJob();
        var secondImageJob = CreateImageJob();
        
        await service.AddToQueue(firstImageJob, CancellationToken.None);
        await service.AddToQueue(secondImageJob, CancellationToken.None);
        
        // Act
        var firstImageJobResult = await service.RemoveFromQueue(CancellationToken.None);
        var secondImageJobResult = await service.RemoveFromQueue(CancellationToken.None);
        
        // Assert
        Assert.Same(firstImageJob, firstImageJobResult);
        Assert.Same(secondImageJob, secondImageJobResult);
    }

    [Fact]
    public async Task QueueService_DequeueAsync_ThrowsWhenCancelled()
    {
        // Arrange
        var service = new ImgQueueService();
        using var cts = new CancellationTokenSource();
        
        cts.Cancel();
        
        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.RemoveFromQueue(cts.Token).AsTask());
    }
    
}