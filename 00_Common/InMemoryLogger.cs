namespace DotNetConf2024.Common;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
public static class LoggingBuilderExtensions
{
    public static ILoggingBuilder AddInMemory(this ILoggingBuilder builder)
    {
        builder.Services.AddSingleton<StatsService>();
        builder.Services.AddSingleton<InMemoryLogger>();
        builder.Services.AddSingleton<ILoggerProvider, InMemLoggerProvider>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<InMemoryLogger>();
            return new InMemLoggerProvider(logger);
        });
        return builder;
    }

    public static ILoggingBuilder ConfigureAppLogging(this ILoggingBuilder builder)
    {
        builder.ClearProviders();
        builder.AddFilter("System.Net.Http", LogLevel.Error);
        //builder.SetMinimumLevel(LogLevel.Trace);
        builder.AddFilter("DotNetConf2024", LogLevel.Debug);
        builder.AddFilter("Program", LogLevel.Debug);
        builder.AddDebug();
        builder.AddInMemory();
        return builder;
    }
}

public class InMemLoggerProvider : ILoggerProvider
{
    private readonly InMemoryLogger logger;

    public InMemLoggerProvider(InMemoryLogger logger) => this.logger = logger;

    public ILogger CreateLogger(string categoryName) => logger;

    public void Dispose() { }
}

public class InMemoryLogger(StatsService statsService) : ILogger
{
    private readonly List<LogEvent> _logLines = new List<LogEvent>();
    private readonly StatsService _statsService = statsService;

    //private readonly Func<StatsService> _statsService = statsService;

    public IEnumerable<LogEvent> RecordedLogs => _logLines.AsReadOnly();
    public IEnumerable<LogEvent> RecordedTraceLogs => _logLines.Where(l => l.Level == LogLevel.Trace);
    public IEnumerable<LogEvent> RecordedDebugLogs => _logLines.Where(l => l.Level == LogLevel.Debug);
    public IEnumerable<LogEvent> RecordedInformationLogs => _logLines.Where(l => l.Level == LogLevel.Information);
    public IEnumerable<LogEvent> RecordedWarningLogs => _logLines.Where(l => l.Level == LogLevel.Warning);
    public IEnumerable<LogEvent> RecordedErrorLogs => _logLines.Where(l => l.Level == LogLevel.Error);
    public IEnumerable<LogEvent> RecordedCriticalLogs => _logLines.Where(l => l.Level == LogLevel.Critical);
    public IEnumerable<LogEvent> LatestLogs => _logLines.AsReadOnly().OrderByDescending(l => l.Timestamp).Take(20);

    public IDisposable BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        var msg = formatter(state, exception);
        bool addToList = true;

        if (msg.Contains("RetryStrategy', Operation Key: '', Result: '200', Handled: 'False'", StringComparison.CurrentCultureIgnoreCase))
        {
            addToList = false;
        }

        if (msg.Contains("EventName: 'OnRetry'", StringComparison.CurrentCultureIgnoreCase))
        {
            _statsService.Retries++;
        }

        if (msg.Contains("Handled: 'True'", StringComparison.CurrentCultureIgnoreCase))
        {
            _statsService.HandledFailures++;
        }

        if (msg.Contains("MealDbClient-chaos//Chaos.Fault", StringComparison.CurrentCultureIgnoreCase))
        {
            _statsService.ChaosFault++;
        }

        if (msg.Contains("MealDbClient-chaos//Chaos.Outcome", StringComparison.CurrentCultureIgnoreCase))
        {
            _statsService.ChaosErrorOutcome++;
        }

        if (msg.Contains("MealDbClient-chaos//Chaos.Latency", StringComparison.CurrentCultureIgnoreCase))
        {
            _statsService.ChaosLatency++;
        }

        if (addToList)
        {
            _logLines.Add(new LogEvent(DateTime.Now, logLevel, exception, msg));
        }
    }
}

public record LogEvent(DateTime Timestamp, LogLevel Level, Exception Exception, string Message);