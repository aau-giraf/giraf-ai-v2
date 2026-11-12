namespace Giraf_AI.Services.TTS;

/// <summary>
/// Stores completed WAV files separately from the speech provider.
/// </summary>
public interface IAudioStorage
{
    Task SaveAsync(
        Guid id,
        byte[] wav,
        CancellationToken cancellationToken);

    string GetFilePath(Guid id);
}
