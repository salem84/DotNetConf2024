using Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Reflection;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
IServiceCollection services = builder.Services;

services.AddLogging(builder => builder.AddInMemory().AddConsole());

services.AddHttpClient<MealDbClient>();

var host = builder.Build();

var service = host.Services.GetRequiredService<MealDbClient>();
using var cancellationSource = new CancellationTokenSource();
var cancellationToken = cancellationSource.Token;

static Layout CreateLayout()
{
    var layout = new Layout();

    layout.SplitRows(
        new Layout("Top")
            .SplitColumns(
                new Layout("Left")
                    .SplitRows(
                        new Layout("LeftTop"),
                        new Layout("LeftBottom")),
                new Layout("Right").Ratio(2)),
        new Layout("Bottom"));

    layout["LeftBottom"].Update(
        new Panel("[blink]PRESS ANY KEY TO QUIT[/]")
            .Expand()
            .BorderColor(Color.Yellow)
            .Padding(0, 0));

    layout["Right"].Update(
        new Panel(
            new Table()
                .AddColumns("[blue]Qux[/]", "[green]Corgi[/]")
                .AddRow("9", "8")
                .AddRow("7", "6")
                .Expand())
        .Header("A [yellow]Table[/] in a [blue]Panel[/] (Ratio=2)")
        .Expand());

    layout["Bottom"].Update(
    new Panel(
            new FigletText(Assembly.GetEntryAssembly().GetName().Name.Substring(3)))
        .Header("Some [green]Figlet[/] text " + DateTime.Now)
        .Expand());

    return layout;
}
while(true)
{
    var layout = CreateLayout();
    AnsiConsole.Write(layout);
    Console.ReadKey();
}


//var response = await service.GetRandomMealAsync(cancellationToken);