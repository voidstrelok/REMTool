using Microsoft.AspNetCore.Mvc;

namespace RemTool.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "ok",
            service = "api",
            timestamp = DateTimeOffset.UtcNow,
        });
    }
}
