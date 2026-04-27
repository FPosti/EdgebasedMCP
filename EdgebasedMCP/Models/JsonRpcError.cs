namespace EdgebasedMCP.Models;

public sealed class JsonRpcError
{
    public required int Code { get; init; }

    public required string Message { get; init; }
}
