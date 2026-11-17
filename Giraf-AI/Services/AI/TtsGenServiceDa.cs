using Giraf_AI.DTOs.Providers.TtsProviderDa;

namespace Giraf_AI.Services.AI;

public class TtsGenServiceDa
{
    private readonly HttpClient _TtsGenerationClient;

    public TtsGenServiceDa(IHttpClientFactory httpClientFactory)
    {
        _TtsGenerationClient = httpClientFactory.CreateClient("TtsDa");
    }

    public async Task<string> GenerateTtsAsyncDa(string text)
    {
        var TtsGenerateRequestDa = new TtsProviderRequestDa
        {
            Text = text,
            Speaker = "ida",
            Temperature = 0.8,
            TopP = 0.95,
            TopK = 50,
            MaxTokens = 500,
        };
        using var response = await _TtsGenerationClient.PostAsJsonAsync("v1/audio/speech", TtsGenerateRequestDa);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync()
                     ?? throw new InvalidOperationException("Plapre encountered an error");
        return result;
    }
}