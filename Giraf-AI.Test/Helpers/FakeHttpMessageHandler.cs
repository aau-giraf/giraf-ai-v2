namespace Giraf_AI.Test.Helpers;

public class FakeHttpMessageHandler : HttpMessageHandler
{
    public readonly HttpResponseMessage _response;
    
    public FakeHttpMessageHandler(HttpResponseMessage response)
    {
        _response = response;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_response);
    }

}