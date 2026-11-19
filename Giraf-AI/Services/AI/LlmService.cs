// Integrates with the language-model provider used to enrich image prompts.
using System.Net.Http.Json;
using Giraf_AI.DTOs.Providers.LlmProvider;
using Giraf_AI.HelperClasses;

namespace Giraf_AI.Services.AI;

/// <summary>
/// Calls the language-model provider for prompt enrichment.
/// </summary>

public class LlmService
{
    private readonly HttpClient _llmClient;
    public LlmService(IHttpClientFactory httpClientFactory)
    {
        _llmClient = httpClientFactory.CreateClient("Llm");
    }

    public async Task<string> GenerateLlmAsync(string request, string path)
    {
        var llmGenerateRequest = new LlmProviderRequest
        {
            Prompt = new PromptBuilder(request, true, path).Text,
            Model = "qwen3.5:9b",
            Think = false,
            Stream = false,
            Options = new LlmProviderOptions
            {
                Temperature = 0,
                NumPredict = 50
            }
        };

        using var response = await _llmClient.PostAsJsonAsync("api/generate", llmGenerateRequest);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<LlmProviderResponse>()
                     ?? throw new InvalidOperationException("Ollama returned an empty response.");

        return result.LlmResponse;
    }
}