using System.Net.Http.Json;
using System.Text;

namespace Giraf_AI.Services.TTS;

/// <summary>
/// Adapter for the existing Plapre Docker API. Plapre returns raw
/// signed 16-bit little-endian mono PCM at 24 kHz.
/// </summary>
public sealed class PlapreTtsProvider(HttpClient httpClient) : ITtsProvider
{
    public string Name => "plapre";

    public async Task<byte[]> GenerateWavAsync(
        string text,
        CancellationToken cancellationToken)
    {
        var request = new
        {
            text,
            speaker = "tor",
            temperature = 0.8,
            top_p = 0.95,
            top_k = 50,
            max_tokens = 500
        };

        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            "v1/audio/speech",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        byte[] pcm = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        if (pcm.Length == 0 || pcm.Length % 2 != 0)
        {
            throw new InvalidDataException(
                "Plapre returned empty or malformed 16-bit PCM audio.");
        }

        return ConvertPcmToWav(pcm);
    }

    private static byte[] ConvertPcmToWav(byte[] pcm)
    {
        const int sampleRate = 24_000;
        const short channels = 1;
        const short bitsPerSample = 16;

        // Standard PCM WAV with a 44-byte RIFF/WAVE header.
        const int headerSize = 44;
        if (pcm.Length > int.MaxValue - headerSize)
        {
            throw new InvalidDataException("PCM audio is too large for a WAV file.");
        }

        short blockAlign = (short)(channels * bitsPerSample / 8);
        int byteRate = sampleRate * blockAlign;

        using var stream = new MemoryStream(headerSize + pcm.Length);
        using (var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true))
        {
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + pcm.Length);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);            // PCM format chunk size
            writer.Write((short)1);      // Uncompressed PCM
            writer.Write(channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write(blockAlign);
            writer.Write(bitsPerSample);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(pcm.Length);
            writer.Write(pcm);
            writer.Flush();
        }

        return stream.ToArray();
    }
}
