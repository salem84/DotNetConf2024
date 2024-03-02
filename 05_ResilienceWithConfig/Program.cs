using DotNetConf2024.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Simmy;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
IServiceCollection services = builder.Services;

services.AddLogging(builder => builder.ConfigureAppLogging());
services.AddScoped<LayoutUI>();
var httpClientBuilder = services.AddHttpClient<MealDbClient>();

/*
 * RateLimiter(httpStandardResilienceOptions.RateLimiter)
 * Timeout(httpStandardResilienceOptions.TotalRequestTimeout)
 * Retry(httpStandardResilienceOptions.Retry)
 * CircuitBreaker(httpStandardResilienceOptions.CircuitBreaker)
 * Timeout(httpStandardResilienceOptions.AttemptTimeout);
 */
httpClientBuilder.AddStandardResilienceHandler()
    .Configure((options, serviceProvider) =>
    {
        // Update attempt timeout to 1 second
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(1);
        options.AttemptTimeout.OnTimeout = (args) =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("Attempt timeout!");
            return default;
        };
        // Update circuit breaker to handle transient errors and InvalidOperationException
        options.CircuitBreaker.ShouldHandle = args => args.Outcome switch
        {
            { } outcome when HttpClientResiliencePredicates.IsTransient(outcome) => PredicateResult.True(),
            { Exception: InvalidOperationException } => PredicateResult.True(),
            _ => PredicateResult.False()
        };

        // Default Value MaxRetryAttempts is 3
        // options.Retry.MaxRetryAttempts = 5;

        // Update retry strategy to handle transient errors and InvalidOperationException
        options.Retry.ShouldHandle = args => args.Outcome switch
        {
            { } outcome when HttpClientResiliencePredicates.IsTransient(outcome) => PredicateResult.True(),
            { Exception: InvalidOperationException } => PredicateResult.True(),
            _ => PredicateResult.False()
        };
    });

httpClientBuilder.AddResilienceHandler("chaos", (ResiliencePipelineBuilder<HttpResponseMessage> builder) =>
{

    _ = builder
        .AddChaosLatency(0.2, TimeSpan.FromSeconds(3)) // Add latency to simulate network delays
        .AddChaosFault(0.3, () => new InvalidOperationException("Chaos strategy injection!")) // Inject faults to simulate system errors
        .AddChaosOutcome(0.3, () => new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)); // Simulate server errors
});

var host = builder.Build();

var service = host.Services.GetRequiredService<MealDbClient>();
var layoutUI = host.Services.GetRequiredService<LayoutUI>();

layoutUI.AutoRefreshLayoutUI();

while (true)
{
    var response = await service.GetRandomMealAsync();
    Thread.Sleep(1000);
}