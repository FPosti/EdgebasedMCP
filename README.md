# Edge MCP Latency Prototype

This solution has two small apps:

```text
EdgebasedMCP       = edge simulator + MCP HTTP endpoint
AiAgentSimulator  = client that requests samples and records latency
```

The payload is intentionally tiny:

```json
{
  "sequence": 1,
  "sentAtUtc": "2026-04-27T12:00:00Z",
  "sentAtUnixMilliseconds": 1777291200000
}
```

The server creates the timestamp when the agent asks for a sample. The agent measures:

```text
receivedAtUnixMs - sentAtUnixMilliseconds
```

## Run

Start the edge/MCP server:

```powershell
dotnet run --project .\EdgebasedMCP\EdgebasedMCP.csproj
```

Start the agent in another terminal:

```powershell
dotnet run --project .\AiAgentSimulator\AiAgentSimulator.csproj
```

The agent writes one latency value per line to:

```text
logs/latency.csv
```

Example:

```csv
18
21
17
```

That file is ready to graph as a single-column CSV.

## Config

The MCP URL lives in `EdgebasedMCP/appsettings.json`.

Agent polling interval and CSV path live in `AiAgentSimulator/appsettings.json`:

```json
{
  "AiAgent": {
    "McpServerUrl": "http://localhost:5050/mcp",
    "RequestIntervalSeconds": 15,
    "TelemetryToolName": "get_latency_sample"
  },
  "LatencyCsv": {
    "Path": "logs/latency.csv"
  }
}
```

## Useful Test Requests

Manual requests are in:

```text
EdgebasedMCP/EdgebasedMCP.http
```
