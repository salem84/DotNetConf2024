using Polly.Retry;
using Polly.Telemetry;

internal sealed class CustomMeteringEnricher : MeteringEnricher
{
    public override void Enrich<TResult, TArgs>(in EnrichmentContext<TResult, TArgs> context)
    {
        if (context.TelemetryEvent.Arguments is OnRetryArguments<TResult> retryArgs)
        {
            context.Tags.Add(new("retry.attempt", retryArgs.AttemptNumber));
            context.Tags.Add(new("retry.outcome", retryArgs.Outcome));
            context.Tags.Add(new("retry.duration", retryArgs.Duration));
            context.Tags.Add(new("retry.retryDelay", retryArgs.RetryDelay));

        }
    }
}