using System.ComponentModel.DataAnnotations;
using MyCal.Domain.Entity;
using MyCal.Domain.Enum;
using MyCal.Shared.Tracking;

namespace MyCal.Application.Calculators;

public static class NutritionMath
{
    public static decimal Maintenance(decimal kg, decimal cm, int age, Gender coefficient, ActivityLevel activity)
    {
        if (kg is < 30 or > 350 || cm is < 100 or > 250 || age is < 18 or > 100)
            throw new ValidationException("Starting estimates are available for adults with valid metric measurements.");
        var adjustment = coefficient switch
        {
            Gender.Male => 5m,
            Gender.Female => -161m,
            _ => throw new ValidationException("Select a valid equation coefficient.")
        };
        var factor = activity switch
        {
            ActivityLevel.Sedentary => 1.2m,
            ActivityLevel.LightlyActive => 1.375m,
            ActivityLevel.ModeratelyActive => 1.55m,
            ActivityLevel.VeryActive => 1.725m,
            ActivityLevel.ExtraActive => 1.9m,
            _ => throw new ValidationException("Select a valid activity level.")
        };
        return decimal.Round((10 * kg + 6.25m * cm - 5 * age + adjustment) * factor, 0, MidpointRounding.AwayFromZero);
    }

    public static TargetInput StartingTarget(decimal maintenance, decimal weight, GoalType goal)
    {
        var factor = goal switch
        {
            GoalType.LoseWeight => .85m,
            GoalType.MaintainWeight => 1m,
            GoalType.GainWeight => 1.05m,
            _ => throw new ValidationException("Choose a valid goal.")
        };
        var calories = decimal.Round(maintenance * factor, 0, MidpointRounding.AwayFromZero);
        // A conservative beta default, not a claim that 1,500 kcal suits every adult.
        if (goal == GoalType.LoseWeight)
            calories = Math.Max(1500m, calories);
        var protein = weight * 1.6m;
        var fat = calories * .3m / 9;
        var carbs = Carbohydrates(calories, protein, fat);
        return new()
        {
            Calories = calories,
            Protein = protein,
            Fat = fat,
            Carbohydrates = carbs,
            Goal = goal
        };
    }

    public static decimal Carbohydrates(decimal calories, decimal protein, decimal fat)
    {
        var remaining = calories - protein * 4 - fat * 9;
        if (remaining < 0)
            throw new ValidationException("Protein and fat exceed your calorie target. Adjust your targets manually.");
        return remaining / 4;
    }

    public static void ValidateTarget(TargetInput input)
    {
        Validator.ValidateObject(input, new ValidationContext(input), true);
        if (!Enum.IsDefined(input.Goal))
            throw new ValidationException("Choose a valid goal.");
        var macroCalories = input.Protein * 4 + input.Carbohydrates * 4 + input.Fat * 9;
        if (Math.Abs(macroCalories - input.Calories) > 10)
            throw new ValidationException("Macro targets must add up to your calorie target (within 10 kcal for rounding).");
    }

    public static NutritionTotals Totals(IEnumerable<DiaryEntry> entries)
    {
        var all = entries.ToList();
        return new(all.Sum(x => x.Totals.Calories), all.Sum(x => x.Totals.Protein),
            all.Sum(x => x.Totals.Carbohydrates), all.Sum(x => x.Totals.Fat),
            all.Any(x => x.Totals.ProteinIncomplete), all.Any(x => x.Totals.CarbohydratesIncomplete),
            all.Any(x => x.Totals.FatIncomplete));
    }

    public static NutritionTotals Portion(FoodItem food, decimal amount)
    {
        ValidateFood(food);
        if (amount is < .01m or > 100000)
            throw new ValidationException("Enter a quantity greater than zero, up to 100,000.");
        var scale = amount / food.ReferenceQuantity;
        return new(food.Calories * scale, (food.Protein ?? 0) * scale,
            (food.Carbohydrates ?? 0) * scale, (food.Fat ?? 0) * scale,
            food.Protein is null, food.Carbohydrates is null, food.Fat is null);
    }

    public static void ValidateFood(FoodItem? f)
    {
        if (f is null)
            throw new ValidationException("Choose a food to add.");
        if (f.RequiresBasisConfirmation)
            throw new ValidationException("Confirm the package's nutrition basis before adding this food.");
        if (string.IsNullOrWhiteSpace(f.Name) || f.Name.Length > 200 || f.Brand?.Length > 200 ||
            string.IsNullOrWhiteSpace(f.Source) || f.Source.Length > 100 || f.SourceId?.Length > 200 || f.Barcode?.Length > 32 ||
            !Enum.IsDefined(f.Unit) || f.ReferenceQuantity is < .01m or > 10000 ||
            f.Calories is < 0 or > 100000 || f.Protein is < 0 or > 10000 ||
            f.Carbohydrates is < 0 or > 10000 || f.Fat is < 0 or > 10000)
            throw new ValidationException("Check the food name, reference quantity, units, and non-negative nutrition values.");
    }

    public static DateOnly LocalToday(TimeProvider clock, string zone) => DateOnly.FromDateTime(
        TimeZoneInfo.ConvertTime(clock.GetUtcNow(), TimeZoneInfo.FindSystemTimeZoneById(zone)).DateTime);
}
