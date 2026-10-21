using Giraf_AI.Classes;
using Giraf_AI.Classes.TitleProvider;

namespace Giraf_AI.Services.AI;

public class TitleService
{
    public TitleService(IHttpClientFactory httpClientFactory)
    {
        _titleClient = httpClientFactory.CreateClient("Title");
        
    }
    
    private readonly HttpClient _titleClient;

    public async Task<TitleProviderRes> GenerateTitleAsync(string request)
    {
        var titleGenGenerateRequest = new TitleProviderReq
        {
            Prompt = request
        };

        return new TitleProviderRes
        {
            TitleEn = request
        };
    }
}