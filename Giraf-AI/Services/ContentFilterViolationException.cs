// Signals that provider content filtering rejected a generation request.
namespace Giraf_AI.Services;

// <summary>
// Exception thrown when a content filter violation occurs during image generation.
// </summary>

/// <summary>
/// Represents a provider content-filter rejection.
/// </summary>
public sealed class ContentFilterViolationException()
    : Exception("The prompt contains inappropriate content.");