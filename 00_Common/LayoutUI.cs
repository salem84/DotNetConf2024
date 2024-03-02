namespace DotNetConf2024.Common;

using Microsoft.Extensions.Logging;
using Spectre.Console;

public class LayoutUI(StatsService statsService, InMemoryLogger memoryLogger)
{
    public void AutoRefreshLayoutUI()
    {
        // crea un thread separato che gira in background
        var thread = new Thread(() =>
        {
            while (true)
            {
                UpdateUI();
                Thread.Sleep(2000);
            }
        });

        thread.IsBackground = true;
        thread.Start();
    }
    public void UpdateUI()
    {
        var layout = new Layout();

        layout.SplitRows(
            //new Layout("Title"),
            new Layout("Top")
                .SplitColumns(
                    new Layout("Left")
                        .SplitRows(
                            new Layout("Config"),
                            new Layout("Stats")),
                    new Layout("Results")/*.Ratio(2)*/),
            new Layout("Logs"));

        layout["Config"].Update(
            new Panel(
                new BarChart()
                    .CenterLabel()
                    .AddItem("Latency", statsService.ChaosLatency, Color.Yellow)
                    .AddItem("Fault", statsService.ChaosFault, Color.Red)
                    .AddItem("Error Outcome", statsService.ChaosErrorOutcome, Color.Orange3)
                )
            .PadTop(1)
            .Header("Chaos Injected")
            .Expand()
            );

        layout["Stats"].Update(
            new Panel(
                new BarChart()
                    .CenterLabel()
                    .AddItem("Requests", statsService.TotalRequests, Color.Blue)
                    .AddItem("Retries", statsService.Retries, Color.Gold1)
                    .AddItem("Ev. Successes", statsService.EventualSuccesses, Color.Green)
                    .AddItem("Ev. Failures", statsService.EventualFailures, Color.Red)
                )
            .PadTop(1)
            .Header("Statistics")
            .Expand());


        //layout["Title"].Update(
        //new Panel(
        //        new FigletText(Assembly.GetEntryAssembly().GetName().Name.Substring(3)))
        //    .Expand());

        var latestLogs = memoryLogger.LatestLogs.Select(FormatLogToSpectre);
        layout["Logs"].Update(
            new Panel(new Rows(latestLogs))
                .Header("[green]Logs[/]")
                .Expand());

        var latestResults = statsService.HttpResultEvents.OrderByDescending(x => x.Timestamp).Select(FormatResultToSpectre);
        layout["Results"].Update(
            new Panel(new Rows(latestResults))
                .Header("[green]Results[/]")
                .Expand());

        AnsiConsole.Clear();
        AnsiConsole.Write(layout);
    }

    private Markup FormatResultToSpectre(HttpResultEvent r)
    {
        var colorStatusCode = r.StatusCode switch
        {
            200 and < 300 => "green",
            500 and < 600 => "orange3",
            _ => "red"
        };

        var colorDuration = r.Duration switch
        {
            > 1000 => "orange2",
            _ => "grey"
        };
        return new Markup($"[grey]{r.Timestamp.ToLongTimeString()}[/] [{colorStatusCode}]{r.StatusCode}[/] [grey]{r.Duration}ms[/] [{colorStatusCode}]{r.Result[..Math.Min(30, r.Result.Length)]}[/]");
    }

    private Markup FormatLogToSpectre(LogEvent logEvent)
    {
        return new Markup($"{logEvent.Timestamp.ToString("HH:mm:ss.fff")} {GetLevelMarkup(logEvent.Level)}{logEvent.Message}");
    }

    private string GetLevelMarkup(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => "[italic dim grey]trce:[/] ",
            LogLevel.Debug => "[dim grey]dbug:[/] ",
            LogLevel.Information => "[dim deepskyblue2]info:[/] ",
            LogLevel.Warning => "[bold orange3]warn:[/] ",
            LogLevel.Error => "[bold red]fail:[/] ",
            LogLevel.Critical => "[bold underline red on white]crit:[/] ",
            _ => throw new ArgumentOutOfRangeException(nameof(level))
        };
    }
}
