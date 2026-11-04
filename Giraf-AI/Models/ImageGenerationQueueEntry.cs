namespace Giraf_AI.Models;

public sealed class ImageGenerationQueueEntry
{
    public int QueueId { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string ImageId { get; set; } = string.Empty;
    public int MapId { get; set; }
    public string DatabaseIdType { get; set; } = string.Empty;
    public DateTime Received { get; set; }
}
