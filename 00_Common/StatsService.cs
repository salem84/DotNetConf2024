namespace DotNetConf2024.Common;

public class StatsService
{
    public int TotalRequests { get; set; }
    public int EventualSuccesses { get; set; }
    public int EventualFailures { get; set; }
    public int Retries { get; set; }

    public readonly List<HttpResultEvent> HttpResultEvents = new List<HttpResultEvent>();
}

public record HttpResultEvent(DateTime Timestamp, int StatusCode, long Duration, string Result);