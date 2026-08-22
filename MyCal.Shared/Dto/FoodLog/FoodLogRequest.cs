namespace MyCal.Shared.Dto;

public record FoodLogRequest(
    int UserId,
    string Name,
    string? Brand,
    int QuantityInGrams,
    double CaloriesPer100G,
    double ProteinPer100G,
    double CarbohydratesPer100G,
    double FatsPer100G);