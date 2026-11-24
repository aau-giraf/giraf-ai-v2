using Giraf_AI.DTOs;
using Giraf_AI.Services.AI;

namespace Giraf_AI.Services;

public class TtsService
{
    private readonly TtsGenServiceDa _TtsGenServiceDa;
    private readonly TtsGenServiceEn _TtsGenServiceEn;

    public TtsService(
        TtsGenServiceDa TtsGenServiceDa,
        TtsGenServiceEn TtsGenServiceEn)
    {
        _TtsGenServiceDa = TtsGenServiceDa;
        _TtsGenServiceEn = TtsGenServiceEn;
    }
    
    public async Task<TtsResult> ProcessTtsRequestAsync(TtsRequest req)
    {

        if (req.Lang == "da")
        {
            byte[] rawAudio = await _TtsGenServiceDa.GenerateTtsAsyncDa(req.Text);
            return new TtsResult
            {
                Audio = Convert.ToBase64String(CreateWaveFile(rawAudio)),
                TtsID = req.TtsID,
            };
        }
        else if (req.Lang == "en")
        {
            byte[] rawAudio = await _TtsGenServiceEn.GenerateTtsAsyncEn(req.Text);
            return new TtsResult
            {
                Audio = Convert.ToBase64String(rawAudio),
                TtsID = req.TtsID,
            };
        }
        else throw new NotImplementedException("Not implemented");
    }

    private static byte[] CreateWaveFile(byte[] pcmAudio)
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
