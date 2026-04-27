using System.Globalization;
using System.Text.Json;
using AiAgentSimulator.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiAgentSimulator.Services;

public sealed class AiAgentTelemetryWorker : IHostedService, IDisposable
{
    private readonly IMcpClient _mcpClient;
    private readonly AiAgentOptions _options;
    private readonly ILogger<AiAgentTelemetryWorker> _logger;
    private readonly CancellationTokenSource _shutdown = new();
    private Task? _workerTask;
    private PeriodicTimer? _timer;

    public AiAgentTelemetryWorker(
        IMcpClient mcpClient,
        IOptions<AiAgentOptions> options,
        ILogger<AiAgentTelemetryWorker> logger)
    {
        _mcpClient = mcpClient;
        _options = options.Value;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("AI agent simulator is starting.");
        _logger.LogInformation("MCP server URL: {McpServerUrl}", _options.McpServerUrl);
        _logger.LogInformation("The agent will request latency samples every {RequestIntervalSeconds} seconds.", _options.RequestIntervalSeconds);

        _timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.RequestIntervalSeconds));
        _workerTask = RunAsync(_shutdown.Token);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _shutdown.CancelAsync();

        if (_workerTask is not null)
        {
            await _workerTask.WaitAsync(cancellationToken);
        }
    }

    public void Dispose()
    {
        _shutdown.Dispose();
        _timer?.Dispose();
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        if (_timer is null)
        {
            return;
        }

        try
        {
            do
            {
                await RequestTelemetryAsync(cancellationToken);
            }
            while (await _timer.WaitForNextTickAsync(cancellationToken));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private async Task RequestTelemetryAsync(CancellationToken cancellationToken)
    {
        var requestStartedAt = DateTimeOffset.UtcNow;

        try
        {
            _logger.LogInformation("[AI -> MCP] requesting {TelemetryToolName}", _options.TelemetryToolName);

            var telemetry = await _mcpClient.RequestTelemetryAsync(cancellationToken);

            if (telemetry is null)
            {
                _logger.LogWarning("[MCP -> AI] response did not contain a latency sample");
                return;
            }

            var receivedAt = DateTimeOffset.UtcNow;
            var edgeToAiLatencyMs = receivedAt.ToUnixTimeMilliseconds() - telemetry.SentAtUnixMilliseconds;
            var requestRoundTripMs = (receivedAt - requestStartedAt).TotalMilliseconds;

            _logger.LogInformation(
                "[MCP -> AI] seq={Sequence} sentAtUtc={SentAtUtc:O} edgeToAiLatencyMs={EdgeToAiLatencyMs} requestRoundTripMs={RequestRoundTripMs}",
                telemetry.Sequence,
                telemetry.SentAtUtc,
                edgeToAiLatencyMs,
                Math.Round(requestRoundTripMs, 1).ToString("F1", CultureInfo.InvariantCulture));
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(exception, "[AI] MCP latency sample request failed");
        }
    }
}
