// Represents an image-generation item waiting for provider processing.
namespace Giraf_AI.Models;

/// <summary>
/// Represents an image-generation queue item.
/// </summary>

public class ImgQueue
{
    public string Prompt { get; set; } = null!;
    public string ImageId { get; set; } = null!; // PRIMARY KEY
    public int MapId { get; set; }
    public IdType IdType { get; set; }
    public OriginApp OriginApp { get; set; }
    public DateTime Received { get; set; }
}