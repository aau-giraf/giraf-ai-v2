using Microsoft.AspNetCore.Mvc;

namespace DefaultNamespace;

[ApiController]
[Route("api/tts")]
public class TtsController : ControllerBase
{
    [HttpPost]
    public IActionResult GenerateTts([FromBody] TtsRequest request)
    {
        return Ok(new
        {
            text = request.Text,
            userPreferences = request.UserPreferences
        });
    }

    public class TtsRequest
    {
        public string Text { get; init; } = string.Empty;
        public UserPreferences UserPreferences { get; init; } = new();
    }

    public class UserPreferences
    {
        public string Language { get; init; } = string.Empty;
        public string? Accent { get; init; }
        public string VoiceGender { get; init; } = string.Empty;
    }
}