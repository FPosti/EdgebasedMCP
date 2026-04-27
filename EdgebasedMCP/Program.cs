using EdgebasedMCP.Options;
using EdgebasedMCP.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<McpServerOptions>(
    builder.Configuration.GetSection(McpServerOptions.SectionName));

builder.Services.AddControllers();

builder.Services.AddSingleton<IEdgeDevice, SimulatedEdgeDevice>();
builder.Services.AddSingleton<IMcpMessageHandler, McpMessageHandler>();

var app = builder.Build();

var mcpOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<McpServerOptions>>().Value;

Console.WriteLine("Edge/MCP server started.");
Console.WriteLine("MCP endpoint: {0}", mcpOptions.EndpointUrl);

app.MapControllers();

app.Run();
