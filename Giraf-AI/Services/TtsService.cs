using System.Net.Http.Json;
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
            var RawAudio = await _TtsGenServiceDa.GenerateTtsAsyncDa(req.Text);
            return new TtsResult
            {
                Audio = RawAudio,
                TtsID = req.TtsID,
            };
        }
        else if (req.Lang == "en")
        {
            var RawAudio = await _TtsGenServiceEn.GenerateTtsAsyncEn(req.Text);
            return new TtsResult
            {
                Audio = RawAudio,
                TtsID = req.TtsID,
            };
        }
        else throw new NotImplementedException("Not implemented");
        
      

      
    }
}
