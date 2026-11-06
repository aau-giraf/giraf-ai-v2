namespace Giraf_AI.Models;

public class ImgGenQueue
{
    public int QueueId { get; set; } // SERIAL PRIMARY KEY
    public string Prompt { get; set; } = null!;
    public string TranslatedPrompt { get; set; } = null!;
    public string ImageId { get; set; } = null!;
    public int MapId { get; set; }
    public IdType IdType { get; set; }
    public string? TitleDa { get; set; }
    public string? TitleEn { get; set; }
    public bool? Nsfw { get; set; }
    public DateTime Received { get; set; }
    public DateTime? Finished { get; set; }
}