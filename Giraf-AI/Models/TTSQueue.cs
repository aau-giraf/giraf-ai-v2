// Represents a text-to-speech item waiting for provider processing.
namespace Giraf_AI.Models;

/// <summary>
/// Represents a text-to-speech queue item.
/// </summary>

public class TtsQueue
{
    public string Prompt { get; set; } = null!;
    public int TtsId { get; set; }
    public LangType Lang { get; set; }
    public DateTime Received { get; set; }
}
