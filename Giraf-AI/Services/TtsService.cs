// Coordinates speech requests, persistence, and the asynchronous TTS queue.
using Giraf_AI.DTOs;
using Giraf_AI.Services.AI;

namespace Giraf_AI.Services;

// <summary>
// Service responsible for handling text-to-speech requests
// </summary>

/// <summary>
/// Coordinates text-to-speech generation and its persisted result.
/// </summary>
public class TtsService
{
    // Create private readonly fields for the Danish and English TTS generation services
    private readonly TtsGenServiceDa _TtsGenServiceDa;
    private readonly TtsGenServiceEn _TtsGenServiceEn;

    public TtsService(
        // Inject the Danish and English TTS generation services into the constructor
        TtsGenServiceDa TtsGenServiceDa,
        TtsGenServiceEn TtsGenServiceEn)
    {
        // Assign the injected services to the private fields
        _TtsGenServiceDa = TtsGenServiceDa;
        _TtsGenServiceEn = TtsGenServiceEn;
    }
    
    public async Task<TtsResult> ProcessTtsRequestAsync(TtsRequest req)
    {
        // Determine the language from the request, defaulting to Danish if not specified
        var language = req.Lang?.Trim().ToLowerInvariant() ?? "da";
        
        // Danish TTS is handled by the Plapre model and returned as a PCM file, which is then converted to a WAV file before being returned as a base64 string.
        if (language == "da") 
        {
            byte[] rawAudio = await _TtsGenServiceDa.GenerateTtsAsyncDa(req.Text);
            return new TtsResult
            {
                Text = req.Text,
                TtsID = req.TtsID,
                Audio = Convert.ToBase64String(CreateWaveFile(rawAudio))
            };
        }
        // English TTS is handled by the Kokoro model and returned as a WAV file, which is then returned as a base64 string.
        else if (language == "en")
        {
            byte[] rawAudio = await _TtsGenServiceEn.GenerateTtsAsyncEn(req.Text);
            return new TtsResult
            {
                Text = req.Text,
                TtsID = req.TtsID,
                Audio = Convert.ToBase64String(rawAudio)
            };
        }
        else throw new NotImplementedException("Not implemented");
    }

    // <summary>
    // Converts raw PCM audio data to a WAV file format
    // </summary>
    private static byte[] CreateWaveFile(byte[] pcmAudio) // Pure woodoo magic, don't touch it.
    {
        const short channels = 1;
        const int sampleRate = 24_000;
        const short bitsPerSample = 16;
        const short blockAlign = channels * (bitsPerSample / 8);
        const int byteRate = sampleRate * blockAlign;

        using var stream = new MemoryStream(44 + pcmAudio.Length);
        using var writer = new BinaryWriter(stream);

        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + pcmAudio.Length);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);
        writer.Write("data"u8.ToArray());
        writer.Write(pcmAudio.Length);
        writer.Write(pcmAudio);

        return stream.ToArray();
    }
}
