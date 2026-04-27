using System.Net.Http.Json;
using System.Text.Json;
using AiAgentSimulator.Models;
using AiAgentSimulator.Options;
using Microsoft.Extensions.Options;

namespace AiAgentSimulator.Services;

public sealed class McpClient : IMcpClient, IDisposable
{
    private readonly AiAgentOptions _options;
    private readonly HttpClient _httpClient;

    public McpClient(IOptions<AiAgentOptions> options)
    {
        _options = options.Value;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_options.McpServerUrl)
        };
    }

    public async Task<LatencySample?> RequestTelemetryAsync(CancellationToken cancellationToken)
    {
        var request = new JsonRpcRequest<ToolCallParameters>
        {
            Id = 2,
            Method = "tools/call",
            Params = new ToolCallParameters
            {
                Name = _options.TelemetryToolName
            }
        };

        using var response = await _httpClient.PostAsJsonAsync("", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var mcpResponse = await response.Content.ReadFromJsonAsync<McpToolResponse>(cancellationToken: cancellationToken);

        return mcpResponse?.Result?.StructuredContent;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
