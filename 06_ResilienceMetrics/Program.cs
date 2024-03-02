using DotNetConf2024.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Polly;
using Polly.Retry;
using Polly.Simmy;
using Polly.Telemetry;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
IServiceCollection services = builder.Services;

services.AddLogging(builder => builder.ConfigureAppLogging());
services.AddScoped<LayoutUI>();
var httpClientBuilder = services.AddHttpClient<MealDbClient>();

services.Configure<TelemetryOptions>(options =>
{
    options.LoggerFactory = LoggerFactory.Create(builder => builder.ConfigureAppLogging());
    options.MeteringEnrichers.Add(new CustomMeteringEnricher());
});

httpClientBuilder.AddResilienceHandler("standard", (builder, context) =>
{
    builder
        .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
        {
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .Handle<Exception>(),
            //.HandleResult(r => r.StatusCode == HttpStatusCode.InternalServerError),
            Name = "RetryStrategy",
            MaxRetryAttempts = 5,
            Delay = TimeSpan.FromMilliseconds(200),
            UseJitter = true,
            OnRetry = arg =>
            {
                var logger = context.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogDebug("---- OnRetry Event ---- Current Attempt: {0}", arg.AttemptNumber + 1);
                return default;
            }
        })
        .AddTimeout(TimeSpan.FromSeconds(5));
    //.ConfigureTelemetry(telemetryOptions)
});

httpClientBuilder.AddResilienceHandler("chaos", (ResiliencePipelineBuilder<HttpResponseMessage> builder) =>
{
    // Set the chaos injection rate to 50%
    const double InjectionRate = 0.3;

    _ = builder
        // Add latency to simulate network delays
        .AddChaosLatency(InjectionRate, TimeSpan.FromSeconds(5))
        // Inject faults to simulate system errors
        .AddChaosFault(InjectionRate, () => new InvalidOperationException("Chaos strategy injection!"))
        // Simulate server errors
        //.AddChaosOutcome(InjectionRate, () => new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)) 
        ;
});

services.AddMetrics();
services.AddOpenTelemetry()
    .WithMetrics(opts => opts
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("ResilienceMetrics"))
    .AddMeter("Polly")
    .AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("http://localhost:4318/v1/metrics");
        options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
    }));

var host = builder.Build();

var service = host.Services.GetRequiredService<MealDbClient>();
var layoutUI = host.Services.GetRequiredService<LayoutUI>();
var meterProvider = host.Services.GetRequiredService<MeterProvider>();

//layoutUI.AutoRefreshLayoutUI();

while (true)
{
    layoutUI.UpdateUI();
    var response = await service.GetRandomMealAsync();
    meterProvider.ForceFlush();
    Thread.Sleep(1000);
}

internal sealed class CustomMeteringEnricher : MeteringEnricher
{
    public override void Enrich<TResult, TArgs>(in EnrichmentContext<TResult, TArgs> context)
    {
        if (context.TelemetryEvent.Arguments is OnRetryArguments<TResult> retryArgs)
        {
            context.Tags.Add(new("retry.attempt", retryArgs.AttemptNumber));
        }
    }
}