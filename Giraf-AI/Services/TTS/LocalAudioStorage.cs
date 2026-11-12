namespace Giraf_AI.Services.TTS;

/// <summary>Stores generated WAV files under the ASP.NET content root.</summary>
public sealed class LocalAudioStorage(IWebHostEnvironment environment) : IAudioStorage
{
    public async Task SaveAsync(
        Guid id,
        byte[] wav,
        CancellationToken cancellationToken)
    {
        string path = GetFilePath(id);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        // CreateNew prevents overwriting an existing file even if an ID collides.
        await using var stream = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await stream.WriteAsync(wav, cancellationToken);
    }

    public string GetFilePath(Guid id)
    {
        return Path.Combine(
            environment.ContentRootPath,
            "GeneratedAudio",
            $"{id:N}.wav");
    }
}
