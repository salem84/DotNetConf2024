namespace Common;
public class StatsService
{
    public int TotalRequests { get; set; }
    public int EventualSuccesses { get; set; }
    public int EventualFailures { get; set; }
    public int Retries { get; set; }
}
