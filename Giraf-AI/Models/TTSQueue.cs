namespace Giraf_AI.Models;

public class TtsQueue
{
    public int QueueId { get; set; } // SERIAL PRIMARY KEY
    public string Prompt { get; set; } = null!;
    public int TtsId { get; set; }
    public LangType Lang { get; set; }
    public DateTime Received { get; set; }
}
