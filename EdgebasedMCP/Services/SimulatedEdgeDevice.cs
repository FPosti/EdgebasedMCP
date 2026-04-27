using EdgebasedMCP.Models;

namespace EdgebasedMCP.Services;

public sealed class SimulatedEdgeDevice : IEdgeDevice
{
    private readonly Lock _lock = new();
    private LatencySample _latestTelemetry;
    private long _sequence;

    public SimulatedEdgeDevice()
    {
        var startedAt = DateTimeOffset.UtcNow;
        _latestTelemetry = new LatencySample
        {
            Sequence = 0,
            SentAtUtc = startedAt,
            SentAtUnixMilliseconds = startedAt.ToUnixTimeMilliseconds()
        };
    }

    public LatencySample GetLatestTelemetry()
    {
        lock (_lock)
        {
            return _latestTelemetry;
        }
    }

    public LatencySample PublishNextTelemetry()
    {
        lock (_lock)
        {
            _latestTelemetry = CreateNextTelemetry();
            return _latestTelemetry;
        }
    }

    private LatencySample CreateNextTelemetry()
    {
        var sentAt = DateTimeOffset.UtcNow;

        return new LatencySample
        {
            Sequence = ++_sequence,
            SentAtUtc = sentAt,
            SentAtUnixMilliseconds = sentAt.ToUnixTimeMilliseconds()
        };
    }
}
