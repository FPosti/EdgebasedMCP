using System.Text.Json.Serialization;
using EdgebasedMCP.Options;
using EdgebasedMCP.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<EdgeDeviceOptions>(
    builder.Configuration.GetSection(EdgeDeviceOptions.SectionName));
builder.Services.Configure<McpServerOptions>(
    builder.Configuration.GetSection(McpServerOptions.SectionName));
builder.Services.Configure<FileLoggingOptions>(
    builder.Configuration.GetSection(FileLoggingOptions.SectionName));

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.WriteIndented = true;
    });

builder.Services.AddSingleton<IEdgeDevice, SimulatedEdgeDevice>();
builder.Services.AddSingleton<IMcpMessageHandler, McpMessageHandler>();
builder.Services.AddSingleton<ILoggerProvider, SimpleFileLoggerProvider>();
builder.Services.AddHostedService<EdgeTelemetryPublisher>();

var app = builder.Build();

var mcpOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<McpServerOptions>>().Value;
var edgeOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<EdgeDeviceOptions>>().Value;
var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

logger.LogInformation("Simulated edge device is starting.");
logger.LogInformation("Latency samples will be generated every {TelemetryIntervalSeconds} seconds.", edgeOptions.TelemetryIntervalSeconds);
logger.LogInformation("MCP endpoint for an AI agent: {McpEndpointUrl}", mcpOptions.EndpointUrl);
logger.LogInformation("Start AiAgentSimulator in a second terminal to request latency samples from MCP.");

app.MapControllers();

app.Run();
