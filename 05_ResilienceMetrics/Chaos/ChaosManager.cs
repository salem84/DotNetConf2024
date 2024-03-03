using Polly;
using System.Diagnostics;
using System.Net;

namespace DotNetConf2024.CustomResilienceWithMetrics.Chaos;
public class ChaosManager : IChaosManager
{
    private Stopwatch _stopWatch;
    private double TotalSeconds => _stopWatch.Elapsed.TotalSeconds;
    private Random _random = new Random();
    private bool IsCrashed => (TotalSeconds % 60) > 30;
    private bool IsDegraded => (TotalSeconds % 60) >= 10 && (TotalSeconds % 60) <= 20;


    public ChaosManager()
    {
        _stopWatch = new Stopwatch();
        _stopWatch.Start();
    }

    public ValueTask<bool> IsChaosEnabledAsync(ResilienceContext context)
    {
        if (Environment.GetEnvironmentVariable("CHAOS_ENABLED") == "1")
        {
            return ValueTask.FromResult(true);
        }

        return ValueTask.FromResult(false);
    }

    #region Outcome

    public ValueTask<double> GetOutcomeInjectionRateAsync(ResilienceContext context)
    {
        if (IsCrashed)
        {
            return ValueTask.FromResult(1.0);
        }

        return ValueTask.FromResult(0.0);
    }

    public ValueTask<Outcome<HttpResponseMessage>?> GenerateOutcome(ResilienceContext context)
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        ValueTask<Outcome<HttpResponseMessage>?> outcome = ValueTask.FromResult<Outcome<HttpResponseMessage>?>(Outcome.FromResult(response));
        return outcome;
    }
    #endregion

    #region Fault

    public ValueTask<double> GetFaultInjectionRateAsync(ResilienceContext context)
    {
        if (IsDegraded)
        {
            return ValueTask.FromResult(0.2);
        }

        return ValueTask.FromResult(0.0);
    }

    public ValueTask<Exception?> GenerateFault(ResilienceContext context)
    {
        return ValueTask.FromResult<Exception?>(new InvalidOperationException("Chaos strategy injection!"));
    }

    #endregion
    #region Latency

    public ValueTask<double> GetLatencyInjectionRateAsync(ResilienceContext context)
    {
        if (IsCrashed)
        {
            return ValueTask.FromResult(1.0);
        }

        return ValueTask.FromResult(0.1);
    }

    public ValueTask<TimeSpan> GenerateLatency(ResilienceContext context)
    {
        var latency = _random.NextDouble() * 2;
        if (IsCrashed)
        {
            latency += 1;
        }
        return ValueTask.FromResult(TimeSpan.FromSeconds(latency));
    }

    #endregion
}
