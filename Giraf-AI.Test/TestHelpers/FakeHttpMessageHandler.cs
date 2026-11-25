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

public class FakeHttpMessageHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new(responses);

    public int RequestCount { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        RequestCount++;

        if (!_responses.TryDequeue(out var response))
            throw new InvalidOperationException("No fake response was configured.");

        return Task.FromResult(response);
    }
}