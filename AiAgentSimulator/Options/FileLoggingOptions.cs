using Microsoft.Extensions.Logging;

namespace AiAgentSimulator.Options;

public sealed class FileLoggingOptions
{
    public const string SectionName = "FileLogging";

    public bool Enabled { get; set; } = true;

    public string Path { get; set; } = "logs/ai-agent-simulator.log";

    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

    public bool IncludeFrameworkLogs { get; set; }
}
