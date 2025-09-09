using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.IO;

namespace CarelessWhisperV2.Services.Logging;

public class FileLoggerProvider : ILoggerProvider
{
    private readonly string _logFilePath;
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();

    public FileLoggerProvider()
    {
        var appDataPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "CarelessWhisperV2");
        Directory.CreateDirectory(appFolder);
        
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        _logFilePath = Path.Combine(appFolder, $"application-{today}.log");
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new FileLogger(name, _logFilePath));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }
}

public class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly string _logFilePath;
    private readonly object _lock = new object();

    public FileLogger(string categoryName, string logFilePath)
    {
        _categoryName = categoryName;
        _logFilePath = logFilePath;
    }

    public IDisposable BeginScope<TState>(TState state) => null!;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var message = formatter(state, exception);
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logEntry = $"[{timestamp}] [{logLevel}] [{_categoryName}] {message}";
        
        if (exception != null)
        {
            logEntry += System.Environment.NewLine + exception.ToString();
        }

        lock (_lock)
        {
            try
            {
                File.AppendAllText(_logFilePath, logEntry + System.Environment.NewLine);
            }
            catch
            {
                // Ignore file write errors to prevent logging loops
            }
        }
    }
}
