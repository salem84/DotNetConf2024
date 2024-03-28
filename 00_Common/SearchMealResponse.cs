namespace DotNetConf2024.Common;
using System.Text.Json.Serialization;

public record SearchMealResponse(
    [property: JsonPropertyName("meals")] IEnumerable<Meal> Meals);

public record Meal(
    [property: JsonPropertyName("idmeal"), JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] int Id,
    [property: JsonPropertyName("strMeal")] string Name,
    [property: JsonPropertyName("strDrinkAlternate")] string AlternativeDrink,
    [property: JsonPropertyName("strCategory")] string Category,
    [property: JsonPropertyName("strArea")] string Area,
    [property: JsonPropertyName("strInstructions")] string Instructions,
    [property: JsonPropertyName("strMealThumb")] string Thumbnail,
    [property: JsonPropertyName("strTags")] string Tags);