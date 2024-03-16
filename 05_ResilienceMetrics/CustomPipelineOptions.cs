using Microsoft.Extensions.Http.Resilience;
using System.ComponentModel.DataAnnotations;

namespace DotNetConf2024.CustomResilienceWithMetrics;

internal class CustomPipelineOptions
{
    [Required]
    public HttpTimeoutStrategyOptions Timeout { get; set; } = new();
}
