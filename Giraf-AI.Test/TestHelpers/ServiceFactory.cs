using Giraf_AI.Services;
using Giraf_AI.Services.AI;
using Giraf_AI.Test.Services;

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
}