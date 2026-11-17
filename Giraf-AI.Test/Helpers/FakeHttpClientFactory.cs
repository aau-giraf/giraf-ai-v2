namespace Giraf_AI.Test.Helpers;

public class FakeHttpClientFactory(HttpClient client) : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        return client;
    }
}