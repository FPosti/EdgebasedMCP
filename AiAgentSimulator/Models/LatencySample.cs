namespace AiAgentSimulator.Models;

public sealed class LatencySample
{
    public required long Sequence { get; init; }

    public required DateTimeOffset SentAtUtc { get; init; }

    public required long SentAtUnixMilliseconds { get; init; }
}
