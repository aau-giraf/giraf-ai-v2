namespace Giraf_AI.Services.TTS;

/// <summary>
/// Common contract for every speech engine (Plapre now, another provider later).
/// Implementations must return complete WAV bytes, regardless of native format.
/// </summary>
public interface ITtsProvider
{
    string Name { get; }

    Task<byte[]> GenerateWavAsync(
        string text,
        CancellationToken cancellationToken);
}
