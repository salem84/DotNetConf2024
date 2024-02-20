namespace Common;

using System.Net.Http.Json;
using System.Text.Json.Serialization;

public class MealDbClient
{
    private readonly HttpClient _client;
    public MealDbClient(HttpClient client)
    {
        _client = client;
        _client.BaseAddress = new Uri("https://www.themealdb.com/api/json/v1/1/");
    }
    public async Task<SearchMealResponse> GetRandomMealAsync(CancellationToken cancellationToken)
    => await _client.GetFromJsonAsync<SearchMealResponse>("random.php", cancellationToken) ?? new SearchMealResponse([]);
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