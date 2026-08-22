namespace MyCal.Shared.Dto.FoodLog;

public record FoodLogResult(
    int Id,
    DateOnly Date,
    IReadOnlyList<FoodLogEntryResult> Entries,
    double TotalCalories,
    double TotalProtein,
    double TotalCarbohydrates,
    double TotalFats);
