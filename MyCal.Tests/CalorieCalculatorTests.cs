using MyCal.ApiService.Calculators;
using MyCal.ApiService.Common.Enum;
using MyCal.ApiService.Common.Model;

namespace MyCal.Tests;

[TestClass]
public class CalorieCalculatorTests
{
    private User user = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        // test user
        user = new User
        {
            HeightInCm = 180,
            WeightInKg = 75,
            Age = 30,
            Gender = Gender.Male,
            ActivityLevel = ActivityLevel.Sedentary,
        };
    }

    [TestMethod]
    public void CalculateMaintenanceCalories_ReturnsExpectedValue()
    {
        // expected maintenance calories for a 30-year-old male, 180 cm tall, weighing 75 kg, with a sedentary activity level.
        var expectedResult = 2076d; // BMR = 1,730; maintenance = 1,730 * 1.2.

        // Act
        var result = CalorieCalculator.CalculateMaintenanceCalories(user);

        // Assert: BMR = 1,730; maintenance = 1,730 * 1.2.
        Assert.AreEqual(expectedResult, result);
    }

    [TestMethod]
    public void CalculateGoalCalories_LoseWeightModerate_ReturnsExpectedValue()
    {
        // Arrange
        user.WeightGoal = 70; // target weight
        var pace = GoalPace.Moderate;

        // Act
        var result = CalorieCalculator.CalculateGoalCalories(user, pace);

        // Assert
        var diff = CalorieCalculator.CalculateMaintenanceCalories(user) + result.DailyAdjustment;

        Assert.AreEqual(result.TargetCalories, diff);
    }

    [TestMethod]
    public void CalculateGoalCalories_GainWeightModerate_ReturnsExpectedValue()
    {
        // Arrange
        user.WeightGoal = 80; // target weight
        var pace = GoalPace.Moderate;

        // Act
        var result = CalorieCalculator.CalculateGoalCalories(user, pace);

        // Assert
        var diff = CalorieCalculator.CalculateMaintenanceCalories(user) + result.DailyAdjustment;

        Assert.AreEqual(result.TargetCalories, diff);
        Console.WriteLine($"Target Calories: {result.TargetCalories}, Daily Adjustment: {result.DailyAdjustment}");
    }
}
