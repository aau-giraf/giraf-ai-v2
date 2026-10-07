using Microsoft.AspNetCore.Mvc;

namespace DefaultNamespace;

[ApiController]
[Route("api/tts")]
public class TtsController : ControllerBase
{
    [HttpPost]
    public IActionResult GenerateTts([FromBody] TtsClass.TtsSynthesizeRequest request)
    {
        return Ok(new
        {
            // lydfil
        });
    }
}