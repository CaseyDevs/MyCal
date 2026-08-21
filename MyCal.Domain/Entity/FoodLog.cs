using MyCal.Domain.Abstractions;

namespace MyCal.Domain.Entity;

public class FoodLog : BaseEntity
{
    public int UserId { get; set; }
    public List<FoodLogEntry> Entries { get; set; } = [];
    public double TotalCalories { get; set; }
    public double? TotalProtein { get; set; }
    public double? TotalCarbohydrates { get; set; }
    public double? TotalFats { get; set; }
    public DateOnly Date { get; init; }
}

public static class FoodLogExtensions
{
    extension(FoodLog foodLog)
    {
        public double CalculateTotalCalories() =>
            foodLog.Entries.Sum(entry =>
                entry.Food.Calories * entry.QuantityInGrams / 100d);

        public double CalculateTotalProtein() =>
            foodLog.Entries.Sum(entry =>
                (entry.Food.Protein ?? 0d) * entry.QuantityInGrams / 100d);

        public double CalculateTotalCarbohydrates() =>
            foodLog.Entries.Sum(entry =>
                (entry.Food.Carbohydrates ?? 0d) * entry.QuantityInGrams / 100d);

        public double CalculateTotalFats() =>
            foodLog.Entries.Sum(entry =>
                (entry.Food.Fats ?? 0d) * entry.QuantityInGrams / 100d);
    }
}
