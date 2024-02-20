using Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Simmy;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
IServiceCollection services = builder.Services;

var httpClientBuilder = services.AddHttpClient<MealDbClient>();

httpClientBuilder.AddStandardResilienceHandler()
    .Configure(options =>
    {
        // Update attempt timeout to 1 second
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(1);

        // Update circuit breaker to handle transient errors and InvalidOperationException
        options.CircuitBreaker.ShouldHandle = args => args.Outcome switch
        {
            { } outcome when HttpClientResiliencePredicates.IsTransient(outcome) => PredicateResult.True(),
            { Exception: InvalidOperationException } => PredicateResult.True(),
            _ => PredicateResult.False()
        };

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
    // Set the chaos injection rate to 50%
    const double InjectionRate = 0.5;

    _ = builder
        .AddChaosLatency(InjectionRate, TimeSpan.FromSeconds(5)) // Add latency to simulate network delays
        .AddChaosFault(InjectionRate, () => new InvalidOperationException("Chaos strategy injection!")) // Inject faults to simulate system errors
        .AddChaosOutcome(InjectionRate, () => new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)); // Simulate server errors
});

var host = builder.Build();

// Make resilient HTTP request
var service = host.Services.GetRequiredService<MealDbClient>();

do
{
    var response = await service.GetRandomMealAsync(default);
    Thread.Sleep(1000);
}
while (true);