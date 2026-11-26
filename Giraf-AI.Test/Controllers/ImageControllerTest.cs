using Giraf_AI.DTOs;
using Giraf_AI.Services;
using Giraf_AI.Test.Services.Queue;
using Giraf_AI.Test.TestHelpers;
using Microsoft.AspNetCore.Mvc;

namespace Giraf_AI.Test.Controllers;

public class ImageControllerTest
{
    private static ImageRequest CreateImageRequest(Guid? imgId = null)
    {
        return new ImageRequest
        {
            Prompt = "A Cat",
            ImgID = (imgId ?? Guid.NewGuid()).ToString(),
            MapID = 123,
            IdType = "citizenid",
            OriginApp = "fp"
        };
    }

    [Fact]
    public async Task GetImageRequest_ProcessesValidRequest()
    {
        var imageRequest = CreateImageRequest();
        var queue = new ImgQueueServiceMock();
        var dbFactory = new DbContextFactory();
        var (controller, dbContext) =
            ServiceFactory.CreateImageController(queue, dbFactory.Configure);

        var responseTask = controller.GenerateImage(imageRequest, CancellationToken.None);
        var job = await queue.WaitForAddedJobAsync(CancellationToken.None);
        job.ContentCheck.SetResult();

        var response = await responseTask;

        Assert.IsType<AcceptedResult>(response);
        Assert.Equal(imageRequest.ImgID, job.ImageId);
        Assert.Equal(imageRequest.Prompt, job.Request.Prompt);
        Assert.Equal("fp", job.Request.OriginApp);
        Assert.Equal("citizenid", job.Request.IdType);

        var savedEntry = Assert.Single(dbContext.ImgGenQueues);
        Assert.Equal(imageRequest.ImgID, savedEntry.ImageId);
        Assert.Equal(imageRequest.Prompt, savedEntry.Prompt);
        Assert.Equal(123, savedEntry.MapId);
    }

    [Fact]
    public async Task GetImageRequest_Returns403Forbidden_WhenContentFilterException()
    {
        var imageRequest = CreateImageRequest();
        var queue = new ImgQueueServiceMock();
        var dbFactory = new DbContextFactory();
        var (controller, _) = ServiceFactory.CreateImageController(queue, dbFactory.Configure);

        var responseTask = controller.GenerateImage(imageRequest, CancellationToken.None);
        var job = await queue.WaitForAddedJobAsync(CancellationToken.None);

        job.ContentCheck.SetException(new ContentFilterViolationException());

        var result = Assert.IsType<ObjectResult>(await responseTask);
        Assert.Equal(403, result.StatusCode);
        Assert.Equal("The prompt contains inappropriate content.", result.Value);
    }

    [Fact]
    public async Task GetImageRequest_RejectsDuplicateImageId()
    {
        var imgId = Guid.NewGuid();
        var firstImageRequest = CreateImageRequest(imgId);
        var secondImageRequest = CreateImageRequest(imgId);
        var queue = new ImgQueueServiceMock();
        var dbFactory = new DbContextFactory();
        var (controller, dbContext) = ServiceFactory.CreateImageController(queue, dbFactory.Configure);

        Assert.Equal(firstImageRequest.ImgID, secondImageRequest.ImgID);

        var firstResponseTask = controller.GenerateImage(firstImageRequest, CancellationToken.None);
        var firstJob = await queue.WaitForAddedJobAsync(CancellationToken.None);
        firstJob.ContentCheck.SetResult();

        Assert.IsType<AcceptedResult>(await firstResponseTask);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => controller.GenerateImage(secondImageRequest, CancellationToken.None));

        Assert.Single(dbContext.ImgGenQueues);
    }

    [Fact]
    public async Task GetImageRequest_Cancelled_WhenCancellationToken()
    {
        var imageRequest = CreateImageRequest();
        var queue = new ImgQueueServiceMock();
        var dbFactory = new DbContextFactory();
        var (controller, _) = ServiceFactory.CreateImageController(queue, dbFactory.Configure);
        
        var responseTask = controller.GenerateImage(imageRequest, CancellationToken.None);
        var job = await queue.WaitForAddedJobAsync(CancellationToken.None);
        
        var cts =  new CancellationTokenSource();
        cts.Cancel();
        job.ContentCheck.SetResult();
    }

}