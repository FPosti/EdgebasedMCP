namespace AiAgentSimulator.Options;

public sealed class LatencyCsvOptions
{
    public const string SectionName = "LatencyCsv";

    public string Path { get; set; } = "logs/latency.csv";
}
