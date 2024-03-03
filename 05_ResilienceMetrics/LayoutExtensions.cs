using DotNetConf2024.Common;
using Polly.CircuitBreaker;
using Spectre.Console;

namespace DotNetConf2024.CustomResilienceWithMetrics
{
    public static class LayoutExtensions
    {
        public static LayoutUI AddCircuitState(this LayoutUI layoutUI, CircuitState state)
        {
            //state.Replace("Open", "[red]Open[/]", StringComparison.CurrentCultureIgnoreCase)
            //    .Replace("Closed", "[green]Closed[/]", StringComparison.CurrentCultureIgnoreCase)
            //    .Replace("HalfOpen", "[orange3]HalfOpen[/]", StringComparison.CurrentCultureIgnoreCase);
            var color = state switch
            {
                CircuitState.Open => "red",
                CircuitState.Closed => "green",
                CircuitState.HalfOpen => "orange3",
                _ => "white"
            };
            layoutUI.AddCustomStats(new Markup($"Circuit State: [{color}]{state}[/]"));
            return layoutUI;
        }
    }
}
