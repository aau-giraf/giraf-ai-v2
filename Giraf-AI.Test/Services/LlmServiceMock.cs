namespace Giraf_AI.Test.Services;

public class LlmServiceMock
{
    public Task<string> GenerateLlmAsync(string request, string path)
    {
        return Task.FromResult("Llm returned string");
    }
}