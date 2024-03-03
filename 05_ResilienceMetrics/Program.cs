using DotNetConf2024.Common;
using DotNetConf2024.CustomResilienceWithMetrics.Chaos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Simmy;
using Polly.Simmy.Fault;
using Polly.Simmy.Outcomes;
using Polly.Telemetry;
using System.Net;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
IServiceCollection services = builder.Services;

services.AddLogging(builder => builder.ConfigureAppLogging());
services.AddScoped<LayoutUI>();
services.AddSingleton<IChaosManager, ChaosManager>();

var httpClientBuilder = services.AddHttpClient<MealDbClient>();

services.Configure<TelemetryOptions>(options =>
{
    options.LoggerFactory = LoggerFactory.Create(builder => builder.ConfigureAppLogging());
    options.MeteringEnrichers.Add(new CustomMeteringEnricher());
});

httpClientBuilder.AddResilienceHandler("standard", (builder, context) =>
{
    builder
        //.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
        //{
        //    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
        //        .Handle<Exception>(),
        //    //.HandleResult(r => r.StatusCode == HttpStatusCode.InternalServerError),
        //    Name = "RetryStrategy",
        //    MaxRetryAttempts = 5,
        //    Delay = TimeSpan.FromMilliseconds(200),
        //    UseJitter = true,
        //    OnRetry = arg =>
        //    {
        //        var logger = context.ServiceProvider.GetRequiredService<ILogger<Program>>();
        //        logger.LogDebug("---- OnRetry Event ---- Current Attempt: {AttemptNumber}", arg.AttemptNumber + 1);
        //        return default;
        //    }
        //})
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
        {
            Name = "CircuitBreakerStrategy",
            FailureRatio = 0.1,
            //SamplingDuration = TimeSpan.FromSeconds(60),
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .HandleResult(r => r.StatusCode == HttpStatusCode.InternalServerError),
            BreakDuration = TimeSpan.FromSeconds(30),
            OnOpened = arg =>
            {
                var logger = context.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("---- CircuitBreaker OnOpened Event ----");
                return default;
            },
            OnHalfOpened = arg =>
            {
                var logger = context.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("---- CircuitBreaker OnHalfOpened Event ----");
                return default;
            },
            OnClosed = arg =>
            {
                var logger = context.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("---- CircuitBreaker OnClosed Event ----");
                return default;
            }
        })
        .AddTimeout(TimeSpan.FromSeconds(5));

    // Alternative to services.Configure<TelemetryOptions>()
    //.ConfigureTelemetry(telemetryOptions)
});

httpClientBuilder.AddResilienceHandler("chaos", (ResiliencePipelineBuilder<HttpResponseMessage> builder, ResilienceHandlerContext context) =>
{
    // Get IChaosManager from dependency injection
    var chaosManager = context.ServiceProvider.GetRequiredService<IChaosManager>();

    _ = builder
        //.AddChaosLatency(new ChaosLatencyStrategyOptions
        //{
        //    EnabledGenerator = args => chaosManager.IsChaosEnabledAsync(args.Context),
        //    InjectionRateGenerator = args => chaosManager.GetLatencyInjectionRateAsync(args.Context),
        //    LatencyGenerator = args => chaosManager.GenerateLatency(args.Context)
        //})
        .AddChaosFault(new ChaosFaultStrategyOptions
        {
            EnabledGenerator = args => chaosManager.IsChaosEnabledAsync(args.Context),
            InjectionRateGenerator = args => chaosManager.GetFaultInjectionRateAsync(args.Context),
            FaultGenerator = args => chaosManager.GenerateFault(args.Context)
        })
        .AddChaosOutcome(new ChaosOutcomeStrategyOptions<HttpResponseMessage>
        {
            EnabledGenerator = args => chaosManager.IsChaosEnabledAsync(args.Context),
            InjectionRateGenerator = args => chaosManager.GetOutcomeInjectionRateAsync(args.Context),
            OutcomeGenerator = args => chaosManager.GenerateOutcome(args.Context)
        })
        ;
});

services.AddMetrics();
services.AddOpenTelemetry()
    .WithMetrics(opts => opts
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("ResilienceMetrics"))
    .AddMeter("Polly")
    .AddHttpClientInstrumentation()
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
            context.Tags.Add(new("retry.outcome", retryArgs.Outcome));
            context.Tags.Add(new("retry.duration", retryArgs.Duration));
            context.Tags.Add(new("retry.retryDelay", retryArgs.RetryDelay));

        }
    }
}