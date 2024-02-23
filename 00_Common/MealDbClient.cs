namespace Common;

using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

public class MealDbClient
{
    private readonly HttpClient _client;
    private readonly StatsService _statsService;

    public MealDbClient(HttpClient client, StatsService statsService)
    {
        _client = client;
        _statsService = statsService;
        _client.BaseAddress = new Uri("https://www.themealdb.com/api/json/v1/1/");
    }
    public async Task<SearchMealResponse> GetRandomMealAsync(CancellationToken cancellationToken)
    {
        SearchMealResponse mealResult = new SearchMealResponse([]);
        var watch = Stopwatch.StartNew();
        try
        {
            var responseMessage = await _client.GetAsync("random.php", cancellationToken);
            if (responseMessage.IsSuccessStatusCode)
            {
                mealResult = await responseMessage.Content.ReadFromJsonAsync<SearchMealResponse>() ?? new SearchMealResponse([]);
            }
            string mealName = mealResult.Meals.FirstOrDefault()?.Name ?? string.Empty;
            _statsService.HttpResultEvents.Add(new HttpResultEvent(DateTime.Now, (int)responseMessage.StatusCode, watch.ElapsedMilliseconds, mealName));
            return mealResult;
        }
        catch(Exception ex)
        {
            _statsService.HttpResultEvents.Add(new HttpResultEvent(DateTime.Now, -1, watch.ElapsedMilliseconds, ex.Message));
            return new SearchMealResponse([]);
        }
    }
}


public record SearchMealResponse(
    [property: JsonPropertyName("meals")] IEnumerable<Meal> Meals);

public record Meal(
    [property: JsonPropertyName("idmeal")] int Id,
    [property: JsonPropertyName("strMeal")] string Name,
    [property: JsonPropertyName("strDrinkAlternate")] string AlternativeDrink,
    [property: JsonPropertyName("strCategory")] string Category,
    [property: JsonPropertyName("strArea")] string Area,
    [property: JsonPropertyName("strInstructions")] string Instructions,
    [property: JsonPropertyName("strMealThumb")] string Thumbnail,
    [property: JsonPropertyName("strTags")] string Tags);