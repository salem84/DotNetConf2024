namespace DotNetConf2024.Common;

public class StatsService
{
    public int TotalRequests { get; set; }
    public int EventualSuccesses { get; set; }
    public int EventualFailures { get; set; }
    public int HandledFailures { get; set; }
    public int Retries { get; set; }

    public int ChaosFault { get; set; }
    public int ChaosLatency { get; set; }
    public int ChaosErrorOutcome { get; set; }

    public readonly List<HttpResultEvent> HttpResultEvents = new List<HttpResultEvent>();

    public void AddHttpResultEvent(HttpResultEvent resultEvent)
    {
        HttpResultEvents.Add(resultEvent);

        if (resultEvent.StatusCode == 200) EventualSuccesses++;
        else EventualFailures++;

    }
}

public record HttpResultEvent(DateTime Timestamp, int StatusCode, long Duration, string Result);