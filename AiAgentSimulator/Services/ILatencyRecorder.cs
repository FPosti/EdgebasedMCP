namespace AiAgentSimulator.Services;

public interface ILatencyRecorder
{
    Task RecordAsync(long latencyMilliseconds, CancellationToken cancellationToken);
}
