// Models the language-model response consumed by prompt-building services.
using System.Text.Json.Serialization;

namespace Giraf_AI.DTOs.Providers.LlmProvider;

/// <summary>
/// Contains the language-model provider response.
/// </summary>

public class LlmProviderResponse
{
    [JsonPropertyName("response")] public required string LlmResponse { get; init; }
}