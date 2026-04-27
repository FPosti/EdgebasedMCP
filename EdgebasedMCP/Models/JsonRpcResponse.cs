using System.Text.Json;

namespace EdgebasedMCP.Models;

public sealed class JsonRpcResponse
{
    public string Jsonrpc { get; init; } = "2.0";

    public JsonElement? Id { get; init; }

    public object? Result { get; init; }

    public JsonRpcError? Error { get; init; }
}
