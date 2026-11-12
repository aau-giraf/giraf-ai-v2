using System.Text.Json.Serialization;

namespace Giraf_AI.DTOs.Providers.LlmProvider;

public class LlmProviderResponse
{
    [JsonPropertyName("response")] public required string LlmResponse { get; init; }
}