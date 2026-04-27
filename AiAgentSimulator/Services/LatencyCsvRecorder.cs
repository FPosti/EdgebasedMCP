using System.Globalization;
using AiAgentSimulator.Options;
using Microsoft.Extensions.Options;

namespace AiAgentSimulator.Services;

public sealed class LatencyCsvRecorder : ILatencyRecorder
{
    private readonly string _path;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public LatencyCsvRecorder(IOptions<LatencyCsvOptions> options)
    {
        _path = options.Value.Path;
    }

    public async Task RecordAsync(long latencyMilliseconds, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await File.AppendAllTextAsync(
                _path,
                latencyMilliseconds.ToString(CultureInfo.InvariantCulture) + Environment.NewLine,
                cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }
}
