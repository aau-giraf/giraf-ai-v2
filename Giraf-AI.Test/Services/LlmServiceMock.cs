using Giraf_AI.Services.AI;

namespace Giraf_AI.Test.Services;

public class LlmServiceMock : ILlmService
{
    public string TranslationResponse { get; set; } = "Translated prompt";
    public string ContentFilterResponse { get; set; } = "Appropriate";
    public string TitleDaResponse { get; set; } = "Dansk titel";
    public string TitleEnResponse { get; set; } = "English title";

    public Task<string> GenerateLlmAsync(string prompt, string masterPrompt)
    {
        string response = masterPrompt switch
        {
            "Prompts/TRANSLATOR/Master.txt" => TranslationResponse,
            "Prompts/NSFW/Master.txt" => ContentFilterResponse,
            "Prompts/TITLE/TitleDa.txt" => TitleDaResponse,
            "Prompts/TITLE/TitleEn.txt" => TitleEnResponse,
            _ => throw new ArgumentException("Unknown master prompt")
        };

        return Task.FromResult(response);
    }
}