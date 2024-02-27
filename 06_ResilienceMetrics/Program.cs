using Common;
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
services.AddSingleton<StatsService>();
services.AddScoped<LayoutUI>();
var httpClientBuilder = services.AddHttpClient<MealDbClient>();

services.Configure<TelemetryOptions>(options =>
{
    options.LoggerFactory = LoggerFactory.Create(builder => builder.AddInMemory());
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
                logger.LogWarning("OnRetry, Attempt: {0}", arg.AttemptNumber);
                return default;
            }
        })
        .AddTimeout(TimeSpan.FromSeconds(5));
    //.ConfigureTelemetry(telemetryOptions)
});

httpClientBuilder.AddResilienceHandler("chaos", (ResiliencePipelineBuilder<HttpResponseMessage> builder) =>
{
    // Set the chaos injection rate to 50%
    const double InjectionRate = 0.7;

    _ = builder
                                                                 //.AddChaosLatency(InjectionRate, TimeSpan.FromSeconds(5)) // Add latency to simulate network delays
                                                                 .AddChaosFault(InjectionRate, () => new InvalidOperationException("Chaos strategy injection!")); // Inject faults to simulate system errors
                                                                                                                                                                  //.AddChaosOutcome(InjectionRate, () => new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)); // Simulate server errors
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
var statsService = host.Services.GetRequiredService<StatsService>();
var meterProvider = host.Services.GetRequiredService<MeterProvider>();
using var cancellationSource = new CancellationTokenSource();
var cancellationToken = cancellationSource.Token;

//layoutUI.AutoRefreshLayoutUI();

while (true)
{
    layoutUI.UpdateUI();
    Thread.Sleep(1000);
    statsService.TotalRequests++;
    var response = await service.GetRandomMealAsync(cancellationToken);

    meterProvider.ForceFlush();
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