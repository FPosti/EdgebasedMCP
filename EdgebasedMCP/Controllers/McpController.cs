using System.Text.Json;
using EdgebasedMCP.Models;
using EdgebasedMCP.Services;
using Microsoft.AspNetCore.Mvc;

namespace EdgebasedMCP.Controllers;

[ApiController]
[Route("mcp")]
public sealed class McpController : ControllerBase
{
    private readonly IMcpMessageHandler _messageHandler;

    public McpController(IMcpMessageHandler messageHandler)
    {
        _messageHandler = messageHandler;
    }

    [HttpGet]
    public ActionResult<JsonRpcResponse> Get()
    {
        return StatusCode(
            StatusCodes.Status405MethodNotAllowed,
            new JsonRpcResponse
            {
                Error = new JsonRpcError
                {
                    Code = -32601,
                    Message = "This simple MCP server uses request/response over POST /mcp."
                }
            });
    }

    [HttpPost]
    public async Task<IActionResult> Post(CancellationToken cancellationToken)
    {
        JsonDocument document;

        try
        {
            document = await JsonDocument.ParseAsync(Request.Body, cancellationToken: cancellationToken);
        }
        catch (JsonException)
        {
            return StatusCode(
                StatusCodes.Status400BadRequest,
                new JsonRpcResponse
                {
                    Error = new JsonRpcError
                    {
                        Code = -32700,
                        Message = "Invalid JSON."
                    }
                });
        }

        using (document)
        {
            var result = _messageHandler.Handle(document.RootElement);

            if (result.Response is null)
            {
                return StatusCode(result.StatusCode);
            }

            return StatusCode(result.StatusCode, result.Response);
        }
    }
}
