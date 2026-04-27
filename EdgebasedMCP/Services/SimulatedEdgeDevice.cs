using EdgebasedMCP.Models;

namespace EdgebasedMCP.Services;

public sealed class SimulatedEdgeDevice : IEdgeDevice
{
    private readonly Lock _lock = new();
    private long _sequence;

    public LatencySample CreateSample()
    {
        lock (_lock)
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
}
