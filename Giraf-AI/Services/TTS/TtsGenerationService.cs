namespace Giraf_AI.Services.TTS;

/// <summary>
/// Selects the configured speech provider and returns generated WAV bytes.
/// CORE receives the bytes through the API response; AI does not store them.
/// </summary>
public sealed class TtsGenerationService(
    IEnumerable<ITtsProvider> providers,
    IConfiguration configuration)
{
    public Task<byte[]> GenerateAsync(string text, CancellationToken cancellationToken)
    {
        string providerName = configuration["Tts:DefaultProvider"] ?? "plapre";

        ITtsProvider provider = providers.FirstOrDefault(provider =>
            string.Equals(provider.Name, providerName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"TTS provider '{providerName}' is not registered.");

        return provider.GenerateWavAsync(text, cancellationToken);
    }
}
