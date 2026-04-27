using AiAgentSimulator.Options;
using AiAgentSimulator.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile(
    Path.Combine(AppContext.BaseDirectory, "appsettings.json"),
    optional: false,
    reloadOnChange: true);

builder.Services.Configure<AiAgentOptions>(
    builder.Configuration.GetSection(AiAgentOptions.SectionName));
builder.Services.Configure<LatencyCsvOptions>(
    builder.Configuration.GetSection(LatencyCsvOptions.SectionName));

builder.Services.AddSingleton<IMcpClient, McpClient>();
builder.Services.AddSingleton<ILatencyRecorder, LatencyCsvRecorder>();
builder.Services.AddHostedService<AiAgentTelemetryWorker>();

using var host = builder.Build();

await host.RunAsync();
