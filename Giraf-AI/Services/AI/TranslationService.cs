using System.Net.Http.Json;
using Giraf_AI.DTOs.Providers.TranslationProvider;
using Giraf_AI.HelperClasses;

namespace Giraf_AI.Services.AI;

public class TranslationService
{
    public TranslationService(IHttpClientFactory httpClientFactory)
    {
        _translationClient = httpClientFactory.CreateClient("Translation");
    }

    private readonly HttpClient _translationClient;

    public async Task<TranslationProviderRes> GenerateTranslationAsync(string request)
    {
        var translationGenerateRequest = new TranslationProviderReq
        {
            Prompt = new ReadFromFile(request, true, "Prompts/TRANSLATOR/Master.txt").Text
        };

        using var response = await _translationClient.PostAsJsonAsync("api/generate", translationGenerateRequest);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<TranslationProviderRes>() ?? throw new InvalidOperationException("Ollama returned an empty response.");

        return result;
    }
}