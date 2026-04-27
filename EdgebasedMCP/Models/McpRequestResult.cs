namespace EdgebasedMCP.Models;

public sealed class McpRequestResult
{
    public required int StatusCode { get; init; }

    public JsonRpcResponse? Response { get; init; }
}
