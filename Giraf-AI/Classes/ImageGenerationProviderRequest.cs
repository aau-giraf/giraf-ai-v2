using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DefaultNamespace;

/// <summary>
/// Contains the parameters sent to the image‑generation model container.
/// </summary>
public class ImageGenerationProviderRequest
{
    [Required]
    [JsonPropertyName("prompt")]
    public string Prompt { get; init; } = string.Empty;
    [JsonPropertyName("negative_prompt")]
    public string NegativePrompt { get; init; } = string.Empty;
    [JsonPropertyName("size")]
    public string Size { get; init; } = string.Empty;
    [JsonPropertyName("n")]
    public int NumberImages { get; init; }
    [JsonPropertyName("num_inference_steps")]
    public int InferenceSteps { get; init; }
    [JsonPropertyName("guidance_scale")]
    public double GuidanceScale { get; init; }
    [JsonPropertyName("seed")]
    public int Seed { get; init; }    
}