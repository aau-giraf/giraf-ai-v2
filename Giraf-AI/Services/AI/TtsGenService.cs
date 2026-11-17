namespace Giraf_AI.Services.AI;

public class TtsGenService
{
    private readonly HttpClient _TtsGenerationClient;

    public TtsGenService(IHttpClientFactory httpClientFactory)
    {
        _TtsGenerationClient = httpClientFactory.CreateClient("TtsDa");
    }
}