using DotNetConf2024.Common;
using DotNetConf2024.ResilienceRefit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Simmy;
using Refit;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
IServiceCollection services = builder.Services;

services.AddLogging(builder => builder.ConfigureAppLogging());
services.AddScoped<LayoutUI>();

var httpClientBuilder = services
    .AddRefitClient<IMealDbRefitClient>()
    .ConfigureHttpClient(client =>
    {
        client.BaseAddress = new Uri("https://www.themealdb.com/api/json/v1/1/");
    });


httpClientBuilder.AddStandardResilienceHandler();

services.AddScoped<CustomMealDbRefitClient>();

httpClientBuilder.AddResilienceHandler("chaos", (ResiliencePipelineBuilder<HttpResponseMessage> builder) =>
{
    // Set the chaos injection rate to 30%
    const double InjectionRate = 0.3;

    _ = builder
        .AddChaosLatency(InjectionRate, TimeSpan.FromSeconds(1)) // Add latency to simulate network delays
        .AddChaosOutcome(InjectionRate, () => new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)); // Simulate server errors
});

using IHost host = builder.Build();

using IServiceScope scope = host.Services.CreateScope();

var service = scope.ServiceProvider.GetService<CustomMealDbRefitClient>();
var layoutUI = scope.ServiceProvider.GetRequiredService<LayoutUI>();

while (true)
{
    layoutUI.UpdateUI();
    var response = await service.GetRandomMealAsync();
    Thread.Sleep(1000);
}