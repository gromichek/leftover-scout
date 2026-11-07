using System.Text;
using Microsoft.Extensions.Logging;

namespace LeftoverScout.Core.Logging;

/// <summary>
/// A small logger provider that appends one line per entry to <c>leftoverscout-yyyyMMdd.log</c>.
/// I/O errors are swallowed: logging must never throw into the application.
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    private readonly string _logsFolder;
    private readonly TimeProvider _timeProvider;
    private readonly Lock _writeLock = new();

    /// <summary>Creates a provider that writes into <paramref name="logsFolder"/>.</summary>
    /// <param name="logsFolder">The application's own log folder; created on first write.</param>
    /// <param name="timeProvider">Clock; defaults to the system clock.</param>
    public FileLoggerProvider(string logsFolder, TimeProvider? timeProvider = null)
    {
        _logsFolder = logsFolder;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => new FileLogger(this, categoryName);

    /// <inheritdoc />
    public void Dispose()
    {
        // Nothing to release: every entry is written and closed immediately.
    }

    /// <summary>Formats one log entry as a single line (without line break).</summary>
    internal static string FormatLine(DateTimeOffset timestamp, LogLevel level, string category, string message, Exception? exception)
    {
        var builder = new StringBuilder();
        builder.Append(timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture))
            .Append(" [").Append(level).Append("] ")
            .Append(category).Append(": ")
            .Append(SingleLine(message));

        if (exception is not null)
        {
            builder.Append(" | ").Append(SingleLine(exception.ToString()));
        }

        return builder.ToString();
    }

    private static string SingleLine(string text) =>
        text.Replace("\r\n", " | ", StringComparison.Ordinal)
            .Replace("\n", " | ", StringComparison.Ordinal)
            .Replace("\r", " | ", StringComparison.Ordinal);

    private void Write(LogLevel level, string category, string message, Exception? exception)
    {
        try
        {
            DateTimeOffset now = _timeProvider.GetLocalNow();
            string line = FormatLine(now, level, category, message, exception) + Environment.NewLine;

            lock (_writeLock)
            {
                Directory.CreateDirectory(_logsFolder);
                File.AppendAllText(Path.Combine(_logsFolder, LogRetention.GetFileName(now)), line, Utf8NoBom);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            // Logging must never throw into the app.
        }
    }

    private sealed class FileLogger : ILogger
    {
        private readonly FileLoggerProvider _provider;
        private readonly string _category;

        public FileLogger(FileLoggerProvider provider, string category)
        {
            _provider = provider;
            _category = category;
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            _provider.Write(logLevel, _category, formatter(state, exception), exception);
        }
    }
}
