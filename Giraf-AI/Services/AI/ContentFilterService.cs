using Giraf_AI.DTOs.Providers.FilterProvider;

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
                new FilterProviderMessage
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
                new FilterProviderResult
                {
                    IsAllowed = "Yes",
                    Safety = "NSFW",
                    Categories = "Sexual Content"
                }
            ]
        });
    }
}