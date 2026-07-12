using MyCal.ApiService.Common.Enum;
using MyCal.ApiService.Common.Model;

namespace MyCal.ApiService.Calculators;

public static class CalorieCalculator
{
    /// <summary>
    /// Calculates the Basal Metabolic Rate (BMR) based on the user's properties.
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static double CalculateBmr(User user) => user.Gender switch
    {
        Gender.Male =>
            (10 * user.WeightInKg) + (6.25 * user.HeightInCm) - (5 * user.Age) + 5,

        Gender.Female =>
            (10 * user.WeightInKg) + (6.25 * user.HeightInCm) - (5 * user.Age) - 161,

        _ => throw new ArgumentOutOfRangeException(nameof(user.Gender))
    };

    /// <summary>
    /// Calculates the maintenance calories based on the user's BMR and activity level.
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static double CalculateMaintenanceCalories(User user)
    {
        var multiplier = user.ActivityLevel switch
        {
            ActivityLevel.Sedentary => 1.2,
            ActivityLevel.LightlyActive => 1.375,
            ActivityLevel.ModeratelyActive => 1.55,
            ActivityLevel.VeryActive => 1.725,
            ActivityLevel.ExtraActive => 1.9,
            _ => throw new ArgumentOutOfRangeException(nameof(user.ActivityLevel))
        };

        return CalculateBmr(user) * multiplier;
    }    
}