using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<SimulatedEdgeDevice>();
builder.Services.AddHostedService<EdgeTelemetryPublisher>();

var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = true
};

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = jsonOptions.PropertyNamingPolicy;
    options.SerializerOptions.DefaultIgnoreCondition = jsonOptions.DefaultIgnoreCondition;
    options.SerializerOptions.WriteIndented = jsonOptions.WriteIndented;
});

var app = builder.Build();

Console.WriteLine("Simulated edge device is starting.");
Console.WriteLine("Telemetry will be generated every 5 seconds and printed here.");
Console.WriteLine("MCP endpoint for an AI agent: http://localhost:5050/mcp");
Console.WriteLine("Start AiAgentSimulator in a second terminal to request telemetry from MCP.");
Console.WriteLine();

app.MapGet("/", () => Results.Ok("Simulated edge device is running. Watch the command window for telemetry."));

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "simulated-edge-device"
}));

app.MapGet("/telemetry", (SimulatedEdgeDevice device) => Results.Ok(device.GetLatestTelemetry()));

app.MapGet("/mcp", () => Results.Json(
    RpcError(null, -32601, "This simple MCP server uses request/response over POST /mcp."),
    jsonOptions,
    statusCode: StatusCodes.Status405MethodNotAllowed));

app.MapPost("/mcp", async (HttpContext context, SimulatedEdgeDevice device) =>
{
    JsonDocument document;

    try
    {
        document = await JsonDocument.ParseAsync(context.Request.Body, cancellationToken: context.RequestAborted);
    }
    catch (JsonException)
    {
        return Results.Json(
            RpcError(null, -32700, "Invalid JSON."),
            jsonOptions,
            statusCode: StatusCodes.Status400BadRequest);
    }

    using (document)
    {
        var request = document.RootElement;

        if (request.ValueKind != JsonValueKind.Object ||
            !request.TryGetProperty("jsonrpc", out var jsonRpc) ||
            jsonRpc.GetString() != "2.0" ||
            !request.TryGetProperty("method", out var methodElement) ||
            methodElement.ValueKind != JsonValueKind.String)
        {
            return Results.Json(
                RpcError(null, -32600, "Expected a JSON-RPC 2.0 request with a method."),
                jsonOptions,
                statusCode: StatusCodes.Status400BadRequest);
        }

        var id = request.TryGetProperty("id", out var idElement)
            ? idElement.Clone()
            : (JsonElement?)null;

        if (id is null)
        {
            return Results.Accepted();
        }

        var method = methodElement.GetString();
        var parameters = request.TryGetProperty("params", out var paramsElement)
            ? paramsElement
            : default;

        object response = method switch
        {
            "initialize" => RpcResult(id, new
            {
                protocolVersion = "2025-11-25",
                capabilities = new
                {
                    resources = new { listChanged = false },
                    tools = new { listChanged = false }
                },
                serverInfo = new
                {
                    name = "simulated-edge-device",
                    title = "Simulated Edge Device",
                    version = "1.0.0"
                },
                instructions = "Read edge://telemetry/latest or call get_temperature_telemetry to receive timestamped temperature telemetry."
            }),
            "ping" => RpcResult(id, new { }),
            "resources/list" => RpcResult(id, new
            {
                resources = new[]
                {
                    new
                    {
                        uri = "edge://telemetry/latest",
                        name = "latest_temperature_telemetry",
                        title = "Latest Temperature Telemetry",
                        description = "Simulated edge telemetry with timestamp and temperature reading.",
                        mimeType = "application/json"
                    }
                }
            }),
            "resources/read" => ReadTelemetryResource(id, parameters, device),
            "tools/list" => RpcResult(id, new
            {
                tools = new[]
                {
                    new
                    {
                        name = "get_temperature_telemetry",
                        title = "Get Temperature Telemetry",
                        description = "Returns one simulated edge telemetry sample with timestamp and temperature.",
                        inputSchema = new
                        {
                            type = "object",
                            properties = new { },
                            additionalProperties = false
                        },
                        annotations = new
                        {
                            readOnlyHint = true
                        }
                    }
                }
            }),
            "tools/call" => CallTelemetryTool(id, parameters, device),
            _ => RpcError(id, -32601, $"Unknown MCP method '{method}'.")
        };

        return Results.Json(response, jsonOptions, contentType: "application/json");
    }
});

app.Run();

static object ReadTelemetryResource(JsonElement? id, JsonElement parameters, SimulatedEdgeDevice device)
{
    if (parameters.ValueKind != JsonValueKind.Object ||
        !parameters.TryGetProperty("uri", out var uriElement) ||
        uriElement.GetString() != "edge://telemetry/latest")
    {
        return RpcError(id, -32602, "resources/read expects params.uri to be edge://telemetry/latest.");
    }

    var telemetry = device.GetLatestTelemetry();
    WriteMcpServedLine("resource", telemetry);

    return RpcResult(id, new
    {
        contents = new[]
        {
            new
            {
                uri = "edge://telemetry/latest",
                mimeType = "application/json",
                text = JsonSerializer.Serialize(telemetry, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            }
        }
    });
}

static object CallTelemetryTool(JsonElement? id, JsonElement parameters, SimulatedEdgeDevice device)
{
    if (parameters.ValueKind != JsonValueKind.Object ||
        !parameters.TryGetProperty("name", out var nameElement) ||
        nameElement.GetString() != "get_temperature_telemetry")
    {
        return RpcError(id, -32602, "tools/call expects params.name to be get_temperature_telemetry.");
    }

    var telemetry = device.GetLatestTelemetry();
    WriteMcpServedLine("tool", telemetry);

    return RpcResult(id, new
    {
        content = new[]
        {
            new
            {
                type = "text",
                text = JsonSerializer.Serialize(telemetry, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            }
        },
        structuredContent = telemetry,
        isError = false
    });
}

static object RpcResult(JsonElement? id, object result)
{
    return new
    {
        jsonrpc = "2.0",
        id,
        result
    };
}

static object RpcError(JsonElement? id, int code, string message)
{
    return new
    {
        jsonrpc = "2.0",
        id,
        error = new
        {
            code,
            message
        }
    };
}

static void WriteMcpServedLine(string source, TemperatureTelemetry telemetry)
{
    Console.WriteLine(
        "[MCP -> AI] served {0} seq={1} sentAtUtc={2:O} sentAtUnixMs={3} temperatureCelsius={4}",
        source,
        telemetry.Sequence,
        telemetry.SentAtUtc,
        telemetry.SentAtUnixMilliseconds,
        telemetry.TemperatureCelsius.ToString("F2", CultureInfo.InvariantCulture));
}

public sealed class SimulatedEdgeDevice
{
    private readonly Lock _lock = new();
    private readonly Random _random = new();
    private TemperatureTelemetry _latestTelemetry;
    private long _sequence;
    private double _temperatureCelsius = 22.0;

    public SimulatedEdgeDevice()
    {
        var startedAt = DateTimeOffset.UtcNow;
        _latestTelemetry = new TemperatureTelemetry(
            DeviceId: "sim-edge-001",
            Sequence: 0,
            SentAtUtc: startedAt,
            SentAtUnixMilliseconds: startedAt.ToUnixTimeMilliseconds(),
            TemperatureCelsius: _temperatureCelsius);
    }

    public TemperatureTelemetry GetLatestTelemetry()
    {
        lock (_lock)
        {
            return _latestTelemetry;
        }
    }

    public TemperatureTelemetry PublishNextTelemetry()
    {
        lock (_lock)
        {
            _latestTelemetry = CreateNextTelemetry();
            return _latestTelemetry;
        }
    }

    private TemperatureTelemetry CreateNextTelemetry()
    {
        var randomChange = (_random.NextDouble() - 0.5) * 0.8;
        _temperatureCelsius = Math.Clamp(_temperatureCelsius + randomChange, 18.0, 30.0);
        var sentAt = DateTimeOffset.UtcNow;

        return new TemperatureTelemetry(
            DeviceId: "sim-edge-001",
            Sequence: ++_sequence,
            SentAtUtc: sentAt,
            SentAtUnixMilliseconds: sentAt.ToUnixTimeMilliseconds(),
            TemperatureCelsius: Math.Round(_temperatureCelsius, 2));
    }
}

public sealed record TemperatureTelemetry(
    string DeviceId,
    long Sequence,
    DateTimeOffset SentAtUtc,
    long SentAtUnixMilliseconds,
    double TemperatureCelsius);

public sealed class EdgeTelemetryPublisher : BackgroundService
{
    private readonly SimulatedEdgeDevice _device;

    public EdgeTelemetryPublisher(SimulatedEdgeDevice device)
    {
        _device = device;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        do
        {
            var telemetry = _device.PublishNextTelemetry();

            Console.WriteLine(
                "[EDGE -> MCP] seq={0} sentAtUtc={1:O} sentAtUnixMs={2} temperatureCelsius={3}",
                telemetry.Sequence,
                telemetry.SentAtUtc,
                telemetry.SentAtUnixMilliseconds,
                telemetry.TemperatureCelsius.ToString("F2", CultureInfo.InvariantCulture));
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
