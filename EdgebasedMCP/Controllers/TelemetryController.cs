using EdgebasedMCP.Models;
using EdgebasedMCP.Services;
using Microsoft.AspNetCore.Mvc;

namespace EdgebasedMCP.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class TelemetryController : ControllerBase
{
    private readonly IEdgeDevice _device;

    public TelemetryController(IEdgeDevice device)
    {
        _device = device;
    }

    [HttpGet]
    public ActionResult<LatencySample> GetLatest()
    {
        return Ok(_device.GetLatestTelemetry());
    }
}
