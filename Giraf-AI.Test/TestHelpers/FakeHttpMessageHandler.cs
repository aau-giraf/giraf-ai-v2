namespace Giraf_AI.Test.TestHelpers;

/*public class FakeHttpMessageHandler : HttpMessageHandler
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
}*/

public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses;
    private readonly Func<HttpResponseMessage>? _responseFactory;

    public FakeHttpMessageHandler(params HttpResponseMessage[] responses)
    {
        _responses = new Queue<HttpResponseMessage>(responses);
    }

    public FakeHttpMessageHandler(Func<HttpResponseMessage> responseFactory)
    {
        _responses = new Queue<HttpResponseMessage>();
        _responseFactory = responseFactory;
    }
    public int RequestCount { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        RequestCount++;

        if (_responseFactory is not null)
        {
            return Task.FromResult(_responseFactory());
        }
        
        if (!_responses.TryDequeue(out var response))
            throw new InvalidOperationException("No fake response was configured.");

        return Task.FromResult(response);
    }
}