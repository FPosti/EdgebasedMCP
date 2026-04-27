namespace EdgebasedMCP.Options;

public sealed class McpServerOptions
{
    public const string SectionName = "McpServer";

    public string EndpointUrl { get; set; } = "http://localhost:5050/mcp";

    public string ToolName { get; set; } = "get_latency_sample";
}
