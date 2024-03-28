namespace DotNetConf2024.ResilienceRefit;

using DotNetConf2024.Common;
using Refit;
using System.Diagnostics;
using System.Diagnostics.Metrics;

public class CustomMealDbRefitClient
{
    private readonly IMealDbRefitClient _client;
    private readonly StatsService _statsService;
    private readonly IMeterFactory _meterFactory;

    public CustomMealDbRefitClient(IMealDbRefitClient client, StatsService statsService, IMeterFactory meterFactory)
    {
        _client = client;
        _statsService = statsService;
        _meterFactory = meterFactory;
    }
    public async Task<SearchMealResponse> GetRandomMealAsync()
    {
        _statsService.TotalRequests++;

        var watch = Stopwatch.StartNew();
        try
        {
            var mealResult = await _client.GetRandomMealAsync();
            string mealName = mealResult.Meals.FirstOrDefault()?.Name ?? string.Empty;
            _statsService.AddHttpResultEvent(new HttpResultEvent(DateTime.Now, 200, watch.ElapsedMilliseconds, mealName));
            return mealResult;
        }
        catch (Exception ex)
        {
            int statusCode = ex is ApiException apiEx ? (int)apiEx.StatusCode : -1;
            _statsService.AddHttpResultEvent(new HttpResultEvent(DateTime.Now, statusCode, watch.ElapsedMilliseconds, ex.Message));
            return new SearchMealResponse([]);
        }
    }
}


public interface IMealDbRefitClient
{
    [Get("/random.php")]
    Task<SearchMealResponse> GetRandomMealAsync();
}
