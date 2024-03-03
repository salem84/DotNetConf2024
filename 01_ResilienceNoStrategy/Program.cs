using DotNetConf2024.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
IServiceCollection services = builder.Services;

services.AddLogging(builder => builder.ConfigureAppLogging());
services.AddScoped<LayoutUI>();
services.AddHttpClient<MealDbClient>();

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