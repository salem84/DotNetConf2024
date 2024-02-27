using Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Polly;
using Polly.Retry;
using Polly.Simmy;
using Polly.Telemetry;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
IServiceCollection services = builder.Services;

services.AddLogging(/*builder => builder.ClearProviders().AddInMemory()*/);
services.AddSingleton<StatsService>();
services.AddScoped<LayoutUI>();
var httpClientBuilder = services.AddHttpClient<MealDbClient>();

var telemetryOptions = new TelemetryOptions
{
    // Configure logging
    LoggerFactory = LoggerFactory.Create(builder => builder.AddConsole())
};
telemetryOptions.MeteringEnrichers.Add(new CustomMeteringEnricher());

httpClientBuilder.AddResilienceHandler("standard", builder =>
{
    builder.AddTimeout(TimeSpan.FromSeconds(1));
    builder.ConfigureTelemetry(telemetryOptions);
}
);

httpClientBuilder.AddResilienceHandler("chaos", (ResiliencePipelineBuilder<HttpResponseMessage> builder) =>
{
    // Set the chaos injection rate to 50%
    const double InjectionRate = 0.5;

    _ = builder
        .AddChaosLatency(InjectionRate, TimeSpan.FromSeconds(5)) // Add latency to simulate network delays
        .AddChaosFault(InjectionRate, () => new InvalidOperationException("Chaos strategy injection!")) // Inject faults to simulate system errors
        .AddChaosOutcome(InjectionRate, () => new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)); // Simulate server errors
});

services.AddOpenTelemetry().WithMetrics(opts => opts
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("ResilienceMetrics"))
                .AddMeter("*")
                .AddConsoleExporter()
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri("http://localhost:4318/v1/metrics");
                }));

var host = builder.Build();

var service = host.Services.GetRequiredService<MealDbClient>();
//var layoutUI = host.Services.GetRequiredService<LayoutUI>();
var statsService = host.Services.GetRequiredService<StatsService>();
using var cancellationSource = new CancellationTokenSource();
var cancellationToken = cancellationSource.Token;

while (true)
{
    //layoutUI.UpdateUI();
    Thread.Sleep(1000);
    statsService.TotalRequests++;
    var response = await service.GetRandomMealAsync(cancellationToken);
}


internal class MyTelemetryListener : TelemetryListener
{
    public override void Write<TResult, TArgs>(in TelemetryEventArguments<TResult, TArgs> args)
    {
        Console.WriteLine($"*****Telemetry event occurred: {args.Event.EventName}");
    }
}

internal sealed class CustomMeteringEnricher : MeteringEnricher
{
    public override void Enrich<TResult, TArgs>(in EnrichmentContext<TResult, TArgs> context)
    {
        // You can read additional details from any resilience event and use it to enrich the telemetry
        if (context.TelemetryEvent.Arguments is OnRetryArguments<TResult> retryArgs)
        {
            // See https://github.com/open-telemetry/semantic-conventions/blob/main/docs/general/metrics.md for more details
            // on how to name the tags.
            context.Tags.Add(new("retry.attempt", retryArgs.AttemptNumber));
        }
    }
}