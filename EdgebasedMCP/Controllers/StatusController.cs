using EdgebasedMCP.Models;
using Microsoft.AspNetCore.Mvc;

namespace EdgebasedMCP.Controllers;

[ApiController]
public sealed class StatusController : ControllerBase
{
    [HttpGet("/")]
    public ActionResult<string> GetRoot()
    {
        return Ok("Simulated edge device is running. Watch the command window for telemetry.");
    }

    [HttpGet("/health")]
    public ActionResult<HealthResponse> GetHealth()
    {
        return Ok(new HealthResponse
        {
            Status = "healthy",
            Service = "simulated-edge-device"
        });
    }
}
