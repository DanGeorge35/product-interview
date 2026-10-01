using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProductService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public sealed class HealthController : ControllerBase
{
    /// <summary>Anonymous health check — returns 200 OK when the service is up.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get() =>
        Ok(new { status = "Healthy", timestamp = DateTimeOffset.UtcNow });
}
