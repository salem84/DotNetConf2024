namespace DotNetConf2024.Common;

using Microsoft.Extensions.Logging;
using Spectre.Console;

public class LayoutUI(StatsService statsService, InMemoryLogger memoryLogger)
{
    private readonly List<Markup> CustomStats = new List<Markup>();
    private readonly Color ColorChaosLatency = Color.Yellow;
    private readonly Color ColorChaosFault = Color.Red;
    private readonly Color ColorChaosErrorOutcome = Color.Orange3;

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
                    .AddItem("Latency", statsService.ChaosLatency, ColorChaosLatency)
                    .AddItem("Fault", statsService.ChaosFault, ColorChaosFault)
                    .AddItem("Error Outcome", statsService.ChaosErrorOutcome, ColorChaosErrorOutcome)
                )
            .PadTop(1)
            .Header("[white]Chaos Injected[/]")
            .Expand()
            );

        layout["Stats"].Update(
            new Panel(
                new Grid().Collapse().AddColumns(1)
                .AddRow(
                    new BarChart()
                        .CenterLabel()
                        .AddItem("Requests", statsService.TotalRequests, Color.Blue)
                        .AddItem("Retries", statsService.Retries, Color.Gold1)
                        .AddItem("Ev. Successes", statsService.EventualSuccesses, Color.Green)
                        .AddItem("Ev. Failures", statsService.EventualFailures, Color.Red)
                    )
                .AddRow(new Rows(CustomStats)))
            .PadTop(1)
            .Header("[white]Statistics[/]")
            .Expand());


        //layout["Title"].Update(
        //new Panel(
        //        new FigletText(Assembly.GetEntryAssembly().GetName().Name.Substring(3)))
        //    .Expand());

        var latestLogs = memoryLogger.LatestLogs.Select(FormatLogToSpectre);
        layout["Logs"].Update(
            new Panel(new Rows(latestLogs))
                .Header("[white]Logs[/]")
                .Expand());

        var latestResults = statsService.HttpResultEvents.OrderByDescending(x => x.Timestamp).Select(FormatResultToSpectre);
        layout["Results"].Update(
            new Panel(new Rows(latestResults))
                .Header("[white]Results[/]")
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
        var message = logEvent.Message
            .Replace("Chaos.OnLatency", $"[{ColorChaosLatency}]Chaos.OnLatency[/]")
            .Replace("Chaos.OnFault", $"[{ColorChaosFault}]Chaos.OnFault[/]")
            .Replace("Chaos.OnOutcome", $"[{ColorChaosErrorOutcome}]Chaos.OnOutcome[/]")
            .Replace("OnCircuitClosed", "[white]OnCircuitClosed[/]")
            .Replace("OnCircuitHalfOpened", "[white]OnCircuitHalfOpened[/]")
            .Replace("OnCircuitOpened", "[white]OnCircuitOpened[/]")
            .Replace("OnRetry", "[white]OnRetry[/]");

        return new Markup($"[grey]{logEvent.Timestamp.ToString("HH:mm:ss.fff")}[/] {GetLevelMarkup(logEvent.Level)}{message}");
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

    public void AddCustomStats(Markup markup)
    {
        CustomStats.Clear();
        CustomStats.Add(markup);
    }
}
