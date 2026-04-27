using EdgebasedMCP.Models;

namespace EdgebasedMCP.Services;

public interface IEdgeDevice
{
    LatencySample CreateSample();
}
