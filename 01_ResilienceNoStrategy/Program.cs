using Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Reflection;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
IServiceCollection services = builder.Services;

services.AddLogging(builder => builder.ClearProviders().AddInMemory());
services.AddSingleton<StatsService>();
services.AddScoped<LayoutUI>();
services.AddHttpClient<MealDbClient>();

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


//var response = await service.GetRandomMealAsync(cancellationToken);