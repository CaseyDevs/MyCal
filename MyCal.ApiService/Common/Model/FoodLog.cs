using MyCal.ApiService.Abstractions;

namespace MyCal.ApiService.Common.Model;

public class FoodLog : DomainEntity
{
    public int UserId { get; set; }
    public HashSet<Food> Foods { get; set; } = [];
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
        public double TotalCalories() => 
            foodLog.Foods.Sum(food => food.Calories);

        public double TotalProtein() => 
            foodLog.Foods.Sum(food => food.Protein ?? 0);

        public double TotalCarbohydrates() => 
            foodLog.Foods.Sum(food => food.Carbohydrates ?? 0);

        public double TotalFats() => 
            foodLog.Foods.Sum(food => food.Fats ?? 0);
    }
}