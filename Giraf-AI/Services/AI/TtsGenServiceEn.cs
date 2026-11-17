using Giraf_AI.DTOs.Providers.TtsProviderEn;

namespace Giraf_AI.Services.AI;


public class TtsGenServiceEn
{
    private readonly HttpClient _TtsGenerationClient;

    public TtsGenServiceEn(IHttpClientFactory httpClientFactory)
    {
        _TtsGenerationClient = httpClientFactory.CreateClient("TtsEn");
    }

    public async Task<string> GenerateTtsAsyncEn(string request)
    {
        var TtsGenerateRequestEn = new TtsProviderRequestEn
        {
            Input = request,
            Accent = "a",
            Voice = "f",
            ResponseFormat = "wav",
        };
        using var response = await _TtsGenerationClient.PostAsJsonAsync("v1/audio/speech", TtsGenerateRequestEn);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync()
                     ?? throw new InvalidOperationException("Kokoro encountered an error");
        return result;
    }
    
}