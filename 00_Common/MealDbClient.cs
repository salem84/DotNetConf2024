namespace DotNetConf2024.Common;

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

public class MealDbClient
{
    private readonly HttpClient _client;
    private readonly StatsService _statsService;
    private readonly IMeterFactory _meterFactory;

    public MealDbClient(HttpClient client, StatsService statsService, IMeterFactory meterFactory)
    {
        _client = client;
        _statsService = statsService;
        _meterFactory = meterFactory;
        _client.BaseAddress = new Uri("https://www.themealdb.com/api/json/v1/1/");
    }
    public async Task<SearchMealResponse> GetRandomMealAsync()
    {
        _statsService.TotalRequests++;


        SearchMealResponse mealResult = new SearchMealResponse([]);
        var watch = Stopwatch.StartNew();
        try
        {
            var responseMessage = await _client.GetAsync("random.php");
            if (responseMessage.IsSuccessStatusCode)
            {
                mealResult = await responseMessage.Content.ReadFromJsonAsync<SearchMealResponse>() ?? new SearchMealResponse([]);
            }
            string mealName = mealResult.Meals.FirstOrDefault()?.Name ?? string.Empty;
            _statsService.AddHttpResultEvent(new HttpResultEvent(DateTime.Now, (int)responseMessage.StatusCode, watch.ElapsedMilliseconds, mealName));
            return mealResult;
        }
        catch (Exception ex)
        {
            _statsService.AddHttpResultEvent(new HttpResultEvent(DateTime.Now, -1, watch.ElapsedMilliseconds, ex.Message));
            return new SearchMealResponse([]);
        }
    }
}