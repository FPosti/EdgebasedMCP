namespace AiAgentSimulator.Models;

public sealed class JsonRpcRequest<TParameters>
{
    public string Jsonrpc { get; init; } = "2.0";

    public required int Id { get; init; }

    public required string Method { get; init; }

    public required TParameters Params { get; init; }
}
