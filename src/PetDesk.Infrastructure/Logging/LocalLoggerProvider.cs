using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace PetDesk.Infrastructure.Logging;

public sealed class LocalLoggerProvider(string directory) : ILoggerProvider
{
    private readonly object _sync = new object();
    private bool _disposed;
    public ILogger CreateLogger(string categoryName) => new LocalLogger(this, categoryName);
    private void Write(string category, LogLevel level, EventId id, string message)
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(directory);
                var path = Path.Combine(directory, "petdesk.jsonl");
                if (File.Exists(path) && new FileInfo(path).Length > 2 * 1024 * 1024)
                {
                    for (var i = 2; i >= 1; i--)
                    {
                        if (File.Exists(path + "." + i))
                        {
                            File.Move(path + "." + i, path + "." + (i + 1), true);
                        }
                    }

                    File.Move(path, path + ".1", true);
                }

                File.AppendAllText(path, JsonSerializer.Serialize(new { utc = DateTime.UtcNow, level = level.ToString(), category, eventId = id.Id, message }) + Environment.NewLine);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }
    }

    public void Dispose()
    {
        lock (_sync)
            _disposed = true;
    }

    private sealed class LocalLogger(LocalLoggerProvider owner, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                owner.Write(category, logLevel, eventId, formatter(state, exception));
            }
        }
    }
}
