using System.Text.Json.Serialization;

namespace Giraf_AI.DTOs.Providers.TranslationProvider;

public class TranslationProviderRes
{
    [JsonPropertyName("response")] public required string PromptEn { get; init; }
}