using MyCal.Domain.Entity;
using MyCal.Shared.Dto.FoodLog;

namespace MyCal.Application.Features;

public static class FoodLogHelpers
{
    public static FoodLog CreateFoodLog(int userId, DateOnly today)
    {
        var newLog = new FoodLog
        {
            UserId = userId,
            Date = today,
        };
        return newLog;
    }

    public static List<FoodLogEntryResult> MapFoodLogEntries(FoodLog log)
    {
        return log.Entries
            .Select(e => new FoodLogEntryResult(
                Id: e.Id,
                Name: e.Food.Name,
                Brand: e.Food.Brand,
                QuantityInGrams: e.QuantityInGrams,
                Calories: CalculateTotal(e.Food.Calories, e.QuantityInGrams),
                Protein: CalculateTotal(e.Food.Protein, e.QuantityInGrams),
                Carbohydrates: CalculateTotal(e.Food.Carbohydrates, e.QuantityInGrams),
                Fats: CalculateTotal(e.Food.Fats, e.QuantityInGrams)))
            .ToList();
    }

    // calculate the total amount based on a given quantity
    private static double CalculateTotal(double? value, int quantityInGrams) =>
        (value ?? 0) * quantityInGrams / 100d;
}