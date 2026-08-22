namespace MyCal.Shared.Dto.FoodLog;

public sealed record FoodLogEntryResult(
    int Id,
    string Name,
    string? Brand,
    int QuantityInGrams,
    double Calories,
    double Protein,
    double Carbohydrates,
    double Fats);
