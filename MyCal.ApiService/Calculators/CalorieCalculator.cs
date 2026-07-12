using MyCal.ApiService.Common.Enum;
using MyCal.ApiService.Common.Model;
using MyCal.ApiService.Common.Record;

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

    /// <summary>
    /// Calculates the target calories based on the user's maintenance calories, goal type, and goal pace.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="goal"></param>
    /// <param name="pace"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static CalorieGoalResult CalculateGoalCalories(
        User user,
        GoalPace pace)
    {
        var maintenance = CalculateMaintenanceCalories(user);
        var goal = DetermineGoalType(user);

        var percentage = pace switch
        {
            GoalPace.Slow => 0.10,
            GoalPace.Moderate => 0.15,
            GoalPace.Fast => 0.20,
            _ => throw new ArgumentOutOfRangeException(nameof(pace))
        };

        var adjustment = goal switch
        {
            GoalType.LoseWeight => -(maintenance * percentage), 
            GoalType.MaintainWeight => 0,
            GoalType.GainWeight => maintenance * percentage,
            _ => throw new ArgumentOutOfRangeException(nameof(goal))
        };

        var targetCalories = maintenance + adjustment;

        return new CalorieGoalResult(
            MaintenanceCalories: maintenance,
            DailyAdjustment: adjustment,
            TargetCalories: targetCalories);
    }
 
    // Determines the goal type based on the user's current weight and weight goal.
    public static GoalType DetermineGoalType(User user) => user.WeightGoal switch
    {
        var goal when goal < user.WeightInKg => GoalType.LoseWeight,
        var goal when goal > user.WeightInKg => GoalType.GainWeight,
        _ => GoalType.MaintainWeight
    };
}