using System.Text.Json.Serialization;

namespace Giraf_AI.DTOs.Providers.TranslationProvider;

public class TranslationProviderReq
{
    [JsonPropertyName("model")] public string Model { get; init; } = "qwen3.5:9b";

    [JsonPropertyName("think")] public bool Think { get; init; } = false;

    [JsonPropertyName("prompt")] public required string Prompt { get; init; }

    [JsonPropertyName("stream")] public bool Stream { get; init; } = false;

    [JsonPropertyName("options")] public TranslationProviderOptions Options { get; init; } = new();
}

public class TranslationProviderOptions
{
    [JsonPropertyName("temperature")] public double Temperature { get; init; } = 0;

    [JsonPropertyName("num_predict")] public int NumPredict { get; init; } = 50;
}