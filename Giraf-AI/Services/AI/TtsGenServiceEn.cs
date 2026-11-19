// Integrates with the English speech provider and handles its callback response.
using Giraf_AI.DTOs.Providers.TtsProviderEn;

namespace Giraf_AI.Services.AI;

/// <summary>
/// Calls the English text-to-speech provider.
/// </summary>


public class TtsGenServiceEn
{
    private readonly HttpClient _TtsGenerationClient;

    public TtsGenServiceEn(IHttpClientFactory httpClientFactory)
    {
        _TtsGenerationClient = httpClientFactory.CreateClient("TtsEn");
    }

    public async Task<byte[]> GenerateTtsAsyncEn(string request)
    {
        var TtsGenerateRequestEn = new TtsProviderRequestEn
        {
            Input = request,
            Accent = "a",
            Voice = "f",
            ResponseFormat = "wav"
        };
        using var response = await _TtsGenerationClient.PostAsJsonAsync("v1/audio/speech", TtsGenerateRequestEn);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsByteArrayAsync();
    }
    
}