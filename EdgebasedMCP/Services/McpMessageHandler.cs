using System.Text.Json;
using EdgebasedMCP.Models;
using EdgebasedMCP.Options;
using Microsoft.Extensions.Options;

namespace EdgebasedMCP.Services;

public sealed class McpMessageHandler : IMcpMessageHandler
{
    private const string JsonRpcVersion = "2.0";

    private readonly IEdgeDevice _device;
    private readonly McpServerOptions _options;

    public McpMessageHandler(
        IEdgeDevice device,
        IOptions<McpServerOptions> options)
    {
        _device = device;
        _options = options.Value;
    }

    public McpRequestResult Handle(JsonElement request)
    {
        if (!IsValidJsonRpcRequest(request, out var methodElement))
        {
            return CreateResult(400, CreateError(null, -32600, "Expected a JSON-RPC 2.0 request with a method."));
        }

        var id = request.TryGetProperty("id", out var idElement)
            ? idElement.Clone()
            : (JsonElement?)null;

        if (id is null)
        {
            return new McpRequestResult { StatusCode = StatusCodes.Status202Accepted };
        }

        var parameters = request.TryGetProperty("params", out var paramsElement)
            ? paramsElement
            : default;

        var response = methodElement.GetString() switch
        {
            "tools/call" => CallTelemetryTool(id, parameters),
            var method => CreateError(id, -32601, $"Unknown MCP method '{method}'.")
        };

        return CreateResult(200, response);
    }

    private static bool IsValidJsonRpcRequest(JsonElement request, out JsonElement methodElement)
    {
        methodElement = default;

        return request.ValueKind == JsonValueKind.Object &&
            request.TryGetProperty("jsonrpc", out var jsonRpc) &&
            jsonRpc.GetString() == JsonRpcVersion &&
            request.TryGetProperty("method", out methodElement) &&
            methodElement.ValueKind == JsonValueKind.String;
    }

    private JsonRpcResponse CallTelemetryTool(JsonElement? id, JsonElement parameters)
    {
        if (parameters.ValueKind != JsonValueKind.Object ||
            !parameters.TryGetProperty("name", out var nameElement) ||
            nameElement.GetString() != _options.ToolName)
        {
            return CreateError(id, -32602, $"tools/call expects params.name to be {_options.ToolName}.");
        }

        var telemetry = _device.CreateSample();

        return CreateResult(id, new McpToolCallResult
        {
            StructuredContent = telemetry
        });
    }

    private static JsonRpcResponse CreateResult(JsonElement? id, object result)
    {
        return new JsonRpcResponse
        {
            Id = id,
            Result = result
        };
    }

    private static JsonRpcResponse CreateError(JsonElement? id, int code, string message)
    {
        return new JsonRpcResponse
        {
            Id = id,
            Error = new JsonRpcError
            {
                Code = code,
                Message = message
            }
        };
    }

    private static McpRequestResult CreateResult(int statusCode, JsonRpcResponse response)
    {
        return new McpRequestResult
        {
            StatusCode = statusCode,
            Response = response
        };
    }
}
