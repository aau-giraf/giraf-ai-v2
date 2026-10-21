using Giraf_AI.Classes;
using Giraf_AI.Classes.FilterProvider;

namespace Giraf_AI.Services.AI;

public class ContentFilterService
{
    public ContentFilterService(IHttpClientFactory httpClientFactory)
    {
        _contentFilterClient = httpClientFactory.CreateClient("ContentFilter");
    }
    
    private readonly HttpClient _contentFilterClient;

    public async Task<FilterProviderRes> GenerateContentFilterAsync(string request)
    {
        var contentFilterGenerateRequest = new FilterProviderReq
        {
            Model = "Qwen2.1",
            Messages = [
                new FilterProviderData
                {
                    Role = "user",
                    Content = request
                }
            ]
        };

        return await Task.FromResult(new FilterProviderRes
        {
            Prompt = request,
            Filters = [
                new FilterProviderDataRes
                {
                    IsAllowed = "Yes",
                    Safety = "NSFW",
                    Categories = "Sexual Content"
                }
            ]
        });
    }
}