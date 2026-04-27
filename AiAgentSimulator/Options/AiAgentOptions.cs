namespace AiAgentSimulator.Options;

public sealed class AiAgentOptions
{
    public const string SectionName = "AiAgent";

    public string McpServerUrl { get; set; } = "http://localhost:5050/mcp";

    public int RequestIntervalSeconds { get; set; } = 15;

    public string TelemetryToolName { get; set; } = "get_latency_sample";
}
