namespace DotNetConf2024.CustomResilienceWithMetrics.Chaos;

using Polly;
using System;
using System.Net.Http;

public interface IChaosManager
{
    ValueTask<bool> IsChaosEnabledAsync(ResilienceContext context);

    ValueTask<double> GetFaultInjectionRateAsync(ResilienceContext context);
    ValueTask<Exception?> GenerateFault(ResilienceContext context);
    ValueTask<double> GetOutcomeInjectionRateAsync(ResilienceContext context);
    ValueTask<Outcome<HttpResponseMessage>?> GenerateOutcome(ResilienceContext context);
    ValueTask<double> GetLatencyInjectionRateAsync(ResilienceContext context);
    ValueTask<TimeSpan> GenerateLatency(ResilienceContext context);
}
