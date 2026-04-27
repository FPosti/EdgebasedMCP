using System.Text.Json;
using EdgebasedMCP.Models;

namespace EdgebasedMCP.Services;

public interface IMcpMessageHandler
{
    McpRequestResult Handle(JsonElement request);
}
