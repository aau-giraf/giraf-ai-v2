using Giraf_AI.DTOs.Providers.TranslationProvider;

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
            Prompt = request
        };
        return await Task.FromResult(new TranslationProviderRes
        {
            PromptEn = request
        });
    }
}