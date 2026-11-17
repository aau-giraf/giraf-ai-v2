using Giraf_AI.Services.AI;


namespace Giraf_AI.Test.Helpers;

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
}