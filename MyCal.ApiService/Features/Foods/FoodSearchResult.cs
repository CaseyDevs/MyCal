namespace MyCal.ApiService.Features.Foods;

public sealed record FoodSearchResult(
    string ExternalId,
    string Name,
    string? Brand,
    double CaloriesPer100g,
    double ProteinPer100g,
    double CarbohydratesPer100g,
    double FatPer100g,
    string? Barcode = null,
    string? ImageUrl = null
);