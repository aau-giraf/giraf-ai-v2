using Giraf_AI.DTOs.Providers.TranslationProvider;
using Giraf_AI.HelperClasses;

namespace Giraf_AI.Services.AI;

public class ContentFilterService
{
    public ContentFilterService(IHttpClientFactory httpClientFactory)
    {
        _contentFilterClient = httpClientFactory.CreateClient("ContentFilter");
    }
    
    private readonly HttpClient _contentFilterClient;

    public async Task<TranslationProviderRes> GenerateContentFilterAsync(string request)
    {
        var contentFilterGenerateRequest = new TranslationProviderReq
        {
            Prompt = new ReadFromFile(request, true, "Prompts/NSFW/Master.txt").Text
        };

        using var response = await _contentFilterClient.PostAsJsonAsync("api/generate", contentFilterGenerateRequest);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<TranslationProviderRes>() ?? throw new InvalidOperationException("Ollama returned an empty response.");

        return result;
    }
}