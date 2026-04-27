using AiAgentSimulator.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiAgentSimulator.Services;

public sealed class SimpleFileLoggerProvider : ILoggerProvider
{
    private readonly FileLoggingOptions _options;
    private readonly Lock _lock = new();

    public SimpleFileLoggerProvider(IOptions<FileLoggingOptions> options)
    {
        _options = options.Value;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new SimpleFileLogger(categoryName, _options, _lock);
    }

    public void Dispose()
    {
    }

    private sealed class SimpleFileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly FileLoggingOptions _options;
        private readonly Lock _lock;

        public SimpleFileLogger(string categoryName, FileLoggingOptions options, Lock @lock)
        {
            _categoryName = categoryName;
            _options = options;
            _lock = @lock;
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _options.Enabled &&
                logLevel >= _options.MinimumLevel &&
                (_options.IncludeFrameworkLogs || !_categoryName.StartsWith("Microsoft.", StringComparison.Ordinal));
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter(state, exception);
            if (string.IsNullOrWhiteSpace(message) && exception is null)
            {
                return;
            }

            var logDirectory = System.IO.Path.GetDirectoryName(_options.Path);
            if (!string.IsNullOrWhiteSpace(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            var line = $"{DateTimeOffset.UtcNow:O} [{logLevel}] {_categoryName}: {message}";
            if (exception is not null)
            {
                line = $"{line}{Environment.NewLine}{exception}";
            }

            lock (_lock)
            {
                File.AppendAllText(_options.Path, line + Environment.NewLine);
            }
        }
    }
}
