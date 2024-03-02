using DotNetConf2024.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
httpClientBuilder.AddStandardResilienceHandler();

httpClientBuilder.AddResilienceHandler("chaos", (ResiliencePipelineBuilder<HttpResponseMessage> builder) =>
{
    // Set the chaos injection rate to 30%
    const double InjectionRate = 0.3;

    //TODO CAMBIARE ECCEZIONE LANCIATA
    _ = builder
        .AddChaosLatency(InjectionRate, TimeSpan.FromSeconds(1)) // Add latency to simulate network delays
        .AddChaosOutcome(InjectionRate, () => new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)); // Simulate server errors
});

var host = builder.Build();

var service = host.Services.GetRequiredService<MealDbClient>();
var layoutUI = host.Services.GetRequiredService<LayoutUI>();

while (true)
{
    layoutUI.UpdateUI();
    var response = await service.GetRandomMealAsync();
    Thread.Sleep(1000);
}