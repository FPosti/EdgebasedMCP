using AiAgentSimulator.Models;

namespace AiAgentSimulator.Services;

public interface IMcpClient
{
    Task<LatencySample?> RequestTelemetryAsync(CancellationToken cancellationToken);
}
