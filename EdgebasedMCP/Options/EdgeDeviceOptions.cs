namespace EdgebasedMCP.Options;

public sealed class EdgeDeviceOptions
{
    public const string SectionName = "EdgeDevice";

    public int TelemetryIntervalSeconds { get; set; } = 5;
}
