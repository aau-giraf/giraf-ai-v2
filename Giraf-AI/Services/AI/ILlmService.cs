namespace Giraf_AI.Services.AI;

public interface ILlmService
{
    public Task<string> GenerateLlmAsync(string request, string path);
}