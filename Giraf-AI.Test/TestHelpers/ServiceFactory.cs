using Giraf_AI.Controllers;
using Giraf_AI.Data;
using Giraf_AI.DTOs;
using Giraf_AI.Jobs;
using Giraf_AI.Services;
using Giraf_AI.Services.AI;
using Giraf_AI.Services.Queue;
using Giraf_AI.Test.Services;
using Giraf_AI.Test.Services.Queue;
using Giraf_AI.Test.TestHelpers;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Giraf_AI.Test.TestHelpers;

public class ServiceFactory
{
    public static ImageGenService CreateImageGenService(HttpResponseMessage response)
    {
        var handler = new FakeHttpMessageHandler(response);

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        };

        var factory = new FakeHttpClientFactory(client);

        return new ImageGenService(factory);
    }

    public static LlmService CreateLlmService(HttpResponseMessage response)
    {
        var handler = new FakeHttpMessageHandler(response);

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        };
        var factory = new FakeHttpClientFactory(client);
        
        return new LlmService(factory);
    }
    
    public static ImageService CreateImageService(
        LlmServiceMock llmMock, 
        ImageGenServiceMock imageGenMock)
    {
        return new ImageService(llmMock, imageGenMock);
    }
    
    public static (ServiceProvider, FakeHttpMessageHandler) CreateImgQueueHostedService(
        IQueueService<ImageJob> queue,
        IImageService imageService,
        Action<DbContextOptionsBuilder> configureDb,
        params HttpResponseMessage[] responses)
    {
        return CreateImgQueueHostedService(
            queue, imageService, configureDb, new FakeHttpMessageHandler(responses));
    }

    public static (ServiceProvider, FakeHttpMessageHandler) CreateImgQueueHostedService(
        IQueueService<ImageJob> queue,
        IImageService imageService,
        Action<DbContextOptionsBuilder> configureDb,
        Func<HttpResponseMessage> responseFactory)
    {
        return CreateImgQueueHostedService(
            queue, imageService, configureDb, new FakeHttpMessageHandler(responseFactory));
    }

    private static (ServiceProvider, FakeHttpMessageHandler) CreateImgQueueHostedService(
        IQueueService<ImageJob> queue,
        IImageService imageService,
        Action<DbContextOptionsBuilder> configureDb,
        FakeHttpMessageHandler handler)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddDbContext<AppDbContext>(configureDb);
        services.AddSingleton(handler);
        services.AddSingleton(_ => new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        });
        services.AddSingleton<IHttpClientFactory, FakeHttpClientFactory>();
        services.AddSingleton<IQueueService<ImageJob>>(queue);
        services.AddScoped<IImageService>(_ => imageService);
        services.AddSingleton<ImgQueueHostedService>();

        return (services.BuildServiceProvider(validateScopes: true), handler);
    }
    
    /*private static async Task<ServiceProvider> CreateImageGenerationController(
        ImageRequest imageRequest, 
        Action<DbContextOptionsBuilder> configureDb)
    {
        var request = "";
        var queue = new ImgQueueServiceMock();
        var dbFactory = new DbContextFactory();
        var controller = ServiceFactory.CreateImageController(imageRequest, queue, dbFactory.Configure);

        var responseTask = controller.GenerateImage(request, CancellationToken.None);
        var job = await queue.WaitForAddedJobAsync(CancellationToken.None);
        job.ContentCheck.SetResult();
        
        var services = new ServiceCollection(); 
        services.AddDbContext<AppDbContext>(configureDb);

        return (services.BuildServiceProvider());
    }*/
    
    
    public static (ImageGenerationController Controller, AppDbContext DbContext)
        CreateImageController(
            IQueueService<ImageJob> queue,
            Action<DbContextOptionsBuilder> configureDb)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>();
        configureDb(options);

        var dbContext = new AppDbContext(options.Options);
        var controller = new ImageGenerationController(queue, dbContext);

        return (controller, dbContext);
    }
}