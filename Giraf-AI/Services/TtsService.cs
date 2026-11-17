using System.Net.Http.Json;
using Giraf_AI.DTOs;

namespace Giraf_AI.Services;

public class TtsService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
{
    public async Task<TtsResult> ProcessTtsRequestAsync(TtsRequest req, CancellationToken cancellationToken)
    {
        var ttsClient = httpClientFactory.CreateClient("Tts");
        using var response = await ttsClient.PostAsJsonAsync(
            configuration["Tts:GeneratePath"] ?? "v1/audio/speech",
            new
            {
                text = req.Prompt,
                speaker = "tor",
                temperature = 0.8,
                top_p = 0.95,
                top_k = 50,
                max_tokens = 500
            }, cancellationToken);
        response.EnsureSuccessStatusCode();
        byte[] pcm = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (pcm.Length == 0 || pcm.Length % 2 != 0)
            throw new InvalidOperationException("Plapre returned invalid PCM audio.");

        // Plapre returns signed 16-bit little-endian PCM, 24 kHz, mono.
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write("RIFF"u8);
            writer.Write(36 + pcm.Length);
            writer.Write("WAVE"u8);
            writer.Write("fmt "u8);
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(24000);
            writer.Write(48000);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write("data"u8);
            writer.Write(pcm.Length);
            writer.Write(pcm);
        }

        return new TtsResult
        {
            Audio = Convert.ToBase64String(stream.ToArray()),
            TtsID = req.TtsID,
            Origin = req.Origin
        };
    }
}
