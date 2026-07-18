using MyCal.ApiService.Features.Foods;

namespace MyCal.ApiService.Integrations.USDA;

public sealed class USDAClient(
    HttpClient httpClient,
    IConfiguration configuration)
    : IFoodCatalogClient
{
    private readonly string apiKey =
        configuration["UsdaFoodData:ApiKey"]
        ?? throw new InvalidOperationException(
            "USDA API key is not configured.");

    public async Task<IReadOnlyList<FoodSearchResult>> SearchAsync(
        string query,
        CancellationToken cancellationToken)
    {

        var url =
            $"foods/search" +
            $"?query={Uri.EscapeDataString(query)}" +
            $"&pageSize=10" +
            $"&api_key={apiKey}";

        var response = await httpClient.GetFromJsonAsync<UsdaFoodSearchResponse>(
            url,
            cancellationToken);

        return response?.Foods
            .Where(food => food.FoodNutrients
            .Any(val => val.Value > 0))
            .Select(ToFoodSearchResult)
            .ToList()
            ?? [];
    }

    private static FoodSearchResult ToFoodSearchResult(
        UsdaFoodSearchItem food)
    {
        double GetNutrient(params int[] nutrientIds) =>
            food.FoodNutrients
                .FirstOrDefault(n => nutrientIds.Contains(n.NutrientId))?
                .Value ?? 0;


        return new FoodSearchResult(
            ExternalId: food.FdcId.ToString(),
            Name: food.Description,
            Brand: food.BrandOwner,
            CaloriesPer100g: GetNutrient(1008, 2047, 2048), // calories (energy) id
            ProteinPer100g: GetNutrient(1003), // protein id
            CarbohydratesPer100g: GetNutrient(1005), // carb id
            FatPer100g: GetNutrient(1004)); // fat id
    }

    private sealed record UsdaFoodSearchResponse(
    List<UsdaFoodSearchItem> Foods);

    private sealed record UsdaFoodSearchItem(
        int FdcId,
        string Description,
        string? BrandOwner,
        List<UsdaFoodNutrient> FoodNutrients);

    private sealed record UsdaFoodNutrient(
        int NutrientId,
        string? NutrientName,
        string? NutrientNumber,
        string? UnitName,
        double? Value);
}


