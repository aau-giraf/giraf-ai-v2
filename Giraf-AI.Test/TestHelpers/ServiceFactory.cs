using Giraf_AI.Data;
using Giraf_AI.Jobs;
using Giraf_AI.Services;
using Giraf_AI.Services.AI;
using Giraf_AI.Services.Queue;
using Giraf_AI.Test.Services;
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

    /*public static ImgQueueHostedService CreateImgQueueHostedService(
        IQueueService<ImageJob> queue,
        HttpResponseMessage response,
        IHttpClientFactory factory,
        IImageService imageService)
    {
        var services = new ServiceCollection();

        var handler = new FakeHttpMessageHandler(response);
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        };
        var factory = new FakeHttpClientFactory(client);

        using DbContext dbContext = DbContextFactory.CreateDbContext();
        services.AddScoped<AppDbContext>(_ => DbContextFactory.CreateDbContext());
        services.AddSingleton<IQueueService<ImageJob>>(queue);
        //services.AddSingleton<AppDbContext>(dbContext);
        services.AddSingleton<IHttpClientFactory>(factory);
        services.AddSingleton<ILlmService, LlmServiceMock>();
        services.AddSingleton<IImageGenService, ImageGenServiceMock>();
        services.AddScoped<IImageService, ImageServiceMock>();
        services.AddSingleton<ImgQueueHostedService>();

        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<ImgQueueHostedService>();
    }*/
    
    public static (ServiceProvider, FakeHttpMessageHandler Handler) CreateImgQueueHostedService(
        IQueueService<ImageJob> queue,
        IImageService imageService,
        Action<DbContextOptionsBuilder> configureDb,
        params HttpResponseMessage[] responses)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddDbContext<AppDbContext>(configureDb);

        var handler = new FakeHttpMessageHandler(responses);
        /*services.AddSingleton<HttpClient>(_ => new HttpClient(new FakeHttpMessageHandler(responses))
        {
            BaseAddress = new Uri("http://localhost/")
        });*/
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
}