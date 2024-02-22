namespace Common;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
public static class LoggingBuilderExtensions
{
    public static ILoggingBuilder AddInMemory(this ILoggingBuilder builder)
    {
        var logger = new InMemoryLogger();
        builder.Services.AddSingleton(logger);
        return builder.AddProvider(new InMemLoggerProvider(logger));
    }
}

public class InMemLoggerProvider : ILoggerProvider
{
    private readonly InMemoryLogger logger;

    public InMemLoggerProvider(InMemoryLogger logger) => this.logger = logger;

    public ILogger CreateLogger(string categoryName) => logger;

    public void Dispose() { }
}

public class InMemoryLogger : ILogger
{
    private readonly List<LogEvent> logLines = new List<LogEvent>();

    public IEnumerable<LogEvent> RecordedLogs => this.logLines.AsReadOnly();
    public IEnumerable<LogEvent> RecordedTraceLogs => this.logLines.Where(l => l.Level == LogLevel.Trace);
    public IEnumerable<LogEvent> RecordedDebugLogs => this.logLines.Where(l => l.Level == LogLevel.Debug);
    public IEnumerable<LogEvent> RecordedInformationLogs => this.logLines.Where(l => l.Level == LogLevel.Information);
    public IEnumerable<LogEvent> RecordedWarningLogs => this.logLines.Where(l => l.Level == LogLevel.Warning);
    public IEnumerable<LogEvent> RecordedErrorLogs => this.logLines.Where(l => l.Level == LogLevel.Error);
    public IEnumerable<LogEvent> RecordedCriticalLogs => this.logLines.Where(l => l.Level == LogLevel.Critical);
    public IEnumerable<LogEvent> LatestLogs => this.logLines.AsReadOnly().OrderByDescending(l => l.Timestamp).Take(20);

    public IDisposable BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        this.logLines.Add(new LogEvent(DateTime.Now, logLevel, exception, formatter(state, exception)));
    }
}

public record LogEvent(DateTime Timestamp, LogLevel Level, Exception Exception, string Message);