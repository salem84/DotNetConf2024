using Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Simmy;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
IServiceCollection services = builder.Services;

var httpClientBuilder = services.AddHttpClient<MealDbClient>();

//TODO METTERE COMMENTO CON TIPOLOGIA DI STRATEGIA
httpClientBuilder.AddStandardResilienceHandler();

httpClientBuilder.AddResilienceHandler("chaos", (ResiliencePipelineBuilder<HttpResponseMessage> builder) =>
{
    // Set the chaos injection rate to 50%
    const double InjectionRate = 0.5;

    //TODO CAMBIARE ECCEZIONE LANCIATA
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