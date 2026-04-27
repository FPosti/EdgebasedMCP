using System.Text.Json;
using AiAgentSimulator.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AiAgentSimulator.Services;

public sealed class AiAgentTelemetryWorker : IHostedService, IDisposable
{
    private readonly IMcpClient _mcpClient;
    private readonly ILatencyRecorder _latencyRecorder;
    private readonly AiAgentOptions _options;
    private readonly CancellationTokenSource _shutdown = new();
    private Task? _workerTask;
    private PeriodicTimer? _timer;

    public AiAgentTelemetryWorker(
        IMcpClient mcpClient,
        ILatencyRecorder latencyRecorder,
        IOptions<AiAgentOptions> options)
    {
        _mcpClient = mcpClient;
        _latencyRecorder = latencyRecorder;
        _options = options.Value;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("AI agent started. Writing latency values to CSV.");

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
        try
        {
            var telemetry = await _mcpClient.RequestTelemetryAsync(cancellationToken);

            if (telemetry is null)
            {
                Console.WriteLine("No latency sample received.");
                return;
            }

            var receivedAt = DateTimeOffset.UtcNow;
            var latencyMs = receivedAt.ToUnixTimeMilliseconds() - telemetry.SentAtUnixMilliseconds;

            await _latencyRecorder.RecordAsync(latencyMs, cancellationToken);
            Console.WriteLine(latencyMs);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            Console.WriteLine("Latency request failed: {0}", exception.Message);
        }
    }
}
