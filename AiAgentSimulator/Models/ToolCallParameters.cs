namespace AiAgentSimulator.Models;

public sealed class ToolCallParameters
{
    public required string Name { get; init; }

    public required ToolCallArguments Arguments { get; init; }
}
