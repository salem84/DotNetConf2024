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
                            new Layout("Stats"),
                            new Layout("LeftBottom")),
                    new Layout("Results")/*.Ratio(2)*/),
            new Layout("Logs"));

        layout["LeftBottom"].Update(
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

        var latestLogs = memoryLogger.LatestLogs.Select(l => new Markup(FormatToSpectreText(l)));
        layout["Logs"].Update(
            new Panel(new Rows(latestLogs))
                .Header("[green]Logs[/]")
                .Expand());

        AnsiConsole.Write(layout);
    }

    private string FormatToSpectreText(LogEvent logEvent)
    {
        return $"{logEvent.Timestamp.ToLongTimeString()} {GetLevelMarkup(logEvent.Level)}{logEvent.Message}";
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
