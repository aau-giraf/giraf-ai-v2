using Giraf_AI.DTOs;

namespace Giraf_AI.Services.TTS;

/// <summary>Selects the configured speech provider and stores its WAV output.</summary>
public sealed class TtsGenerationService(
    IEnumerable<ITtsProvider> providers,
    IConfiguration configuration,
    IAudioStorage audioStorage)
{
    public Task<TtsResult> GenerateAsync(
        string text,
        CancellationToken cancellationToken) =>
        GenerateAsync(text, Guid.NewGuid(), cancellationToken);

    public async Task<TtsResult> GenerateAsync(
        string text,
        Guid id,
        CancellationToken cancellationToken)
    {
        string providerName = configuration["Tts:DefaultProvider"] ?? "plapre";

        ITtsProvider provider = providers.FirstOrDefault(provider =>
            string.Equals(provider.Name, providerName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"TTS provider '{providerName}' is not registered.");

        byte[] wav = await provider.GenerateWavAsync(text, cancellationToken);
        await audioStorage.SaveAsync(id, wav, cancellationToken);

        return new TtsResult
        {
            Id = id,
            AudioUrl = $"/api/tts/{id}/audio"
        };
    }
}
