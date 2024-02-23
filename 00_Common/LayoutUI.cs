using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Reflection;

namespace Common;

public class LayoutUI(StatsService statsService, InMemoryLogger memoryLogger)
{
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
            new Panel("[blink]PRESS ANY KEY TO QUIT[/]")
                .Expand()
                .BorderColor(Color.Yellow)
                .Padding(0, 0));

        layout["Stats"].Update(
            new Panel(
                new BarChart()
                    .CenterLabel()
                    .AddItem("Requests", statsService.TotalRequests, Color.Yellow)
                    .AddItem("Retries", statsService.Retries, Color.Red)
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
        return new Markup($"{logEvent.Timestamp.ToLongTimeString()} {GetLevelMarkup(logEvent.Level)}{logEvent.Message}");
    }

    private string GetLevelMarkup(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => "[italic dim grey]trce[/]: ",
            LogLevel.Debug => "[dim grey]dbug[/]: ",
            LogLevel.Information => "[dim deepskyblue2]info[/]: ",
            LogLevel.Warning => "[bold orange3]warn[/]: ",
            LogLevel.Error => "[bold red]fail[/]: ",
            LogLevel.Critical => "[bold underline red on white]crit[/]: ",
            _ => throw new ArgumentOutOfRangeException(nameof(level))
        };
    }
}
