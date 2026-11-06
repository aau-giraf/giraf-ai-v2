using System.Net.Http.Json;
using Giraf_AI.DTOs.Providers.LlmProvider;
using Giraf_AI.HelperClasses;

namespace Giraf_AI.Services.AI;

public class LlmService
{
    public LlmService(IHttpClientFactory httpClientFactory)
    {
        _llmClient = httpClientFactory.CreateClient("Llm");
    }

    private readonly HttpClient _llmClient;

    public async Task<string> GenerateLlmAsync(string request, string path)
    {
        var llmGenerateRequest = new LlmProviderRequest
        {
            Prompt = new ReadFromFile(request, true, path).Text,
            Model = "qwen3.5:4b",
            Think = false,
            Stream = false,
            Options = new LlmProviderOptions
            {
                Temperature = 0,
                NumPredict = 50,
                NumCtx = 200,
                Seed = -1
            }
        };

        using var response = await _llmClient.PostAsJsonAsync("api/generate", llmGenerateRequest);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<LlmProviderResponse>()
                     ?? throw new InvalidOperationException("Ollama returned an empty response.");

        return result.LlmResponse;
    }
}