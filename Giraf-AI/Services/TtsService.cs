using System.Net.Http.Json;
using Giraf_AI.DTOs;

namespace Giraf_AI.Services;

public class TtsService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
{
    public async Task<TtsResult> ProcessTtsRequestAsync(TtsRequest req, CancellationToken cancellationToken)
    {
        var ttsClient = httpClientFactory.CreateClient("Tts");
        // TODO: Confirm model request/response contract with the team.
        using var response = await ttsClient.PostAsJsonAsync(
            configuration["Tts:GeneratePath"] ?? "v1/audio/speech",
            new { text = req.Prompt }, cancellationToken);
        response.EnsureSuccessStatusCode();
        byte[] wav = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (wav.Length == 0)
            throw new InvalidOperationException("TTS returned no audio.");

        return new TtsResult
        {
            Audio = Convert.ToBase64String(wav),
            TtsID = req.TtsID,
            Origin = req.Origin
        };
    }
}
