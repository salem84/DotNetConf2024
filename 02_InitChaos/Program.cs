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

httpClientBuilder.AddResilienceHandler("chaos", (ResiliencePipelineBuilder<HttpResponseMessage> builder) =>
{
    // Set the chaos injection rate to 30%
    const double InjectionRate = 0.3;

    _ = builder
        // Add latency to simulate network delays
        .AddChaosLatency(InjectionRate, TimeSpan.FromSeconds(1))
        // Simulate server errors
        .AddChaosOutcome(InjectionRate, () => new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError))
        ;
});

using IHost host = builder.Build();

using IServiceScope scope = host.Services.CreateScope();

var service = scope.ServiceProvider.GetRequiredService<MealDbClient>();
var layoutUI = scope.ServiceProvider.GetRequiredService<LayoutUI>();

while (true)
{
    layoutUI.UpdateUI();
    var response = await service.GetRandomMealAsync();
    Thread.Sleep(1000);
}