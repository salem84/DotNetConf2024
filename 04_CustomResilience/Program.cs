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

httpClientBuilder.AddStandardResilienceHandler();

httpClientBuilder.AddResilienceHandler("chaos", (ResiliencePipelineBuilder<HttpResponseMessage> builder) =>
{
    // Set the chaos injection rate to 30%
    const double InjectionRate = 0.3;

    _ = builder
        .AddChaosLatency(InjectionRate, TimeSpan.FromSeconds(5)) // Add latency to simulate network delays
        .AddChaosFault(InjectionRate, () => new InvalidOperationException("Chaos strategy injection!")) // Inject faults to simulate system errors
        .AddChaosOutcome(InjectionRate, () => new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)); // Simulate server errors
});

var host = builder.Build();

var service = host.Services.GetRequiredService<MealDbClient>();
var layoutUI = host.Services.GetRequiredService<LayoutUI>();
var statsService = host.Services.GetRequiredService<StatsService>();
using var cancellationSource = new CancellationTokenSource();
var cancellationToken = cancellationSource.Token;

while (true)
{
    layoutUI.UpdateUI();
    Thread.Sleep(1000);
    statsService.TotalRequests++;
    var response = await service.GetRandomMealAsync(cancellationToken);
}