using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

var mcpUrl = GetMcpUrl(args);
using var httpClient = new HttpClient
{
    BaseAddress = new Uri(mcpUrl)
};

Console.WriteLine("AI agent simulator is starting.");
Console.WriteLine("MCP server URL: {0}", mcpUrl);
Console.WriteLine("The agent will request telemetry every 15 seconds.");
Console.WriteLine("Press Ctrl+C to stop.");
Console.WriteLine();

using var shutdown = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    shutdown.Cancel();
};

await InitializeMcpAsync(httpClient, shutdown.Token);

using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));

do
{
    await RequestTelemetryAsync(httpClient, shutdown.Token);
}
while (await timer.WaitForNextTickAsync(shutdown.Token));

static string GetMcpUrl(string[] args)
{
    if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
    {
        return args[0];
    }

    var environmentUrl = Environment.GetEnvironmentVariable("MCP_SERVER_URL");
    if (!string.IsNullOrWhiteSpace(environmentUrl))
    {
        return environmentUrl;
    }

    return "http://localhost:5050/mcp";
}

static async Task InitializeMcpAsync(HttpClient httpClient, CancellationToken cancellationToken)
{
    var request = new
    {
        jsonrpc = "2.0",
        id = 1,
        method = "initialize",
        @params = new
        {
            protocolVersion = "2025-11-25",
            capabilities = new { },
            clientInfo = new
            {
                name = "ai-agent-simulator",
                version = "1.0.0"
            }
        }
    };

    try
    {
        using var response = await httpClient.PostAsJsonAsync("", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        Console.WriteLine("[AI -> MCP] initialize sent");
        Console.WriteLine("[MCP -> AI] initialize response received");
        Console.WriteLine();
    }
    catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
    {
        Console.WriteLine("[AI] Could not initialize MCP connection: {0}", exception.Message);
    }
}

static async Task RequestTelemetryAsync(HttpClient httpClient, CancellationToken cancellationToken)
{
    var requestStartedAt = DateTimeOffset.UtcNow;
    var request = new
    {
        jsonrpc = "2.0",
        id = 2,
        method = "tools/call",
        @params = new
        {
            name = "get_temperature_telemetry",
            arguments = new { }
        }
    };

    try
    {
        Console.WriteLine("[AI -> MCP] requesting get_temperature_telemetry");

        using var response = await httpClient.PostAsJsonAsync("", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var mcpResponse = await response.Content.ReadFromJsonAsync<McpToolResponse>(cancellationToken: cancellationToken);
        var telemetry = mcpResponse?.Result?.StructuredContent;

        if (telemetry is null)
        {
            Console.WriteLine("[MCP -> AI] response did not contain telemetry");
            return;
        }

        var receivedAt = DateTimeOffset.UtcNow;
        var edgeToAiLatencyMs = receivedAt.ToUnixTimeMilliseconds() - telemetry.SentAtUnixMilliseconds;
        var requestRoundTripMs = (receivedAt - requestStartedAt).TotalMilliseconds;

        Console.WriteLine(
            "[MCP -> AI] seq={0} sentAtUtc={1:O} tempC={2} edgeToAiLatencyMs={3} requestRoundTripMs={4}",
            telemetry.Sequence,
            telemetry.SentAtUtc,
            telemetry.TemperatureCelsius.ToString("F2", CultureInfo.InvariantCulture),
            edgeToAiLatencyMs,
            Math.Round(requestRoundTripMs, 1).ToString("F1", CultureInfo.InvariantCulture));
        Console.WriteLine();
    }
    catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
    {
        Console.WriteLine("[AI] MCP telemetry request failed: {0}", exception.Message);
    }
}

public sealed record McpToolResponse(
    string Jsonrpc,
    JsonElement? Id,
    McpToolResult? Result,
    McpError? Error);

public sealed record McpToolResult(
    IReadOnlyList<McpContentItem>? Content,
    TemperatureTelemetry? StructuredContent,
    bool IsError);

public sealed record McpContentItem(
    string Type,
    string Text);

public sealed record TemperatureTelemetry(
    string DeviceId,
    long Sequence,
    DateTimeOffset SentAtUtc,
    long SentAtUnixMilliseconds,
    double TemperatureCelsius);

public sealed record McpError(
    int Code,
    string Message);
