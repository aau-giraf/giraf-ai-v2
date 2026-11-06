using System.Text.Json.Serialization;

namespace Giraf_AI.DTOs.Providers.LlmProvider;

public class LlmProviderRequest
{
    [JsonPropertyName("model")] public required string Model { get; init; }

    [JsonPropertyName("think")] public bool Think { get; init; }

    [JsonPropertyName("prompt")] public required string Prompt { get; init; }

    [JsonPropertyName("stream")] public bool Stream { get; init; }

    [JsonPropertyName("options")] public LlmProviderOptions Options { get; init; } = new();
}

public class LlmProviderOptions
{
    [JsonPropertyName("temperature")] public double Temperature { get; init; }

    [JsonPropertyName("num_predict")] public int NumPredict { get; init; }
    
    [JsonPropertyName("num_ctx")] public int NumCtx { get; init; }
    
    [JsonPropertyName("seed")] public int Seed { get; init; }
}