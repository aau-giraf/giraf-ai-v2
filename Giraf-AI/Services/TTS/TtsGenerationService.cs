using Giraf_AI.DTOs;

namespace Giraf_AI.Services.TTS;

/// <summary>
/// Selects a registered TTS provider and saves the resulting WAV file.
/// The future queue worker can call this service without knowing the provider.
/// </summary>
public sealed class TtsGenerationService(
    IEnumerable<ITtsProvider> providers,
    IConfiguration configuration,
    IAudioStorage audioStorage)
{
    public async Task<TtsResult> GenerateAsync(
        string text,
        CancellationToken cancellationToken)
    {
        string providerName = configuration["Tts:DefaultProvider"] ?? "plapre";

        ITtsProvider provider = providers.FirstOrDefault(provider =>
            string.Equals(
                provider.Name,
                providerName,
                StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"TTS provider '{providerName}' is not registered.");

        byte[] wav = await provider.GenerateWavAsync(text, cancellationToken);

        Guid id = Guid.NewGuid();
        await audioStorage.SaveAsync(id, wav, cancellationToken);

        return new TtsResult
        {
            Id = id,
            AudioUrl = $"/api/tts/{id}/audio"
        };
    }
}
