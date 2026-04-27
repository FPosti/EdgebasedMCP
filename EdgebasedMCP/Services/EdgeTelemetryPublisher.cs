using EdgebasedMCP.Options;
using Microsoft.Extensions.Options;

namespace EdgebasedMCP.Services;

public sealed class EdgeTelemetryPublisher : IHostedService, IDisposable
{
    private readonly IEdgeDevice _device;
    private readonly EdgeDeviceOptions _options;
    private readonly ILogger<EdgeTelemetryPublisher> _logger;
    private readonly CancellationTokenSource _shutdown = new();
    private Task? _publishingTask;
    private PeriodicTimer? _timer;

    public EdgeTelemetryPublisher(
        IEdgeDevice device,
        IOptions<EdgeDeviceOptions> options,
        ILogger<EdgeTelemetryPublisher> logger)
    {
        _device = device;
        _options = options.Value;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.TelemetryIntervalSeconds));
        _publishingTask = PublishAsync(_shutdown.Token);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _shutdown.CancelAsync();

        if (_publishingTask is not null)
        {
            await _publishingTask.WaitAsync(cancellationToken);
        }
    }

    public void Dispose()
    {
        _shutdown.Dispose();
        _timer?.Dispose();
    }

    private async Task PublishAsync(CancellationToken cancellationToken)
    {
        if (_timer is null)
        {
            return;
        }

        try
        {
            while (await _timer.WaitForNextTickAsync(cancellationToken))
            {
                var telemetry = _device.PublishNextTelemetry();

                _logger.LogInformation(
                    "[EDGE -> MCP] seq={Sequence} sentAtUtc={SentAtUtc:O} sentAtUnixMs={SentAtUnixMilliseconds}",
                    telemetry.Sequence,
                    telemetry.SentAtUtc,
                    telemetry.SentAtUnixMilliseconds);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }
}
