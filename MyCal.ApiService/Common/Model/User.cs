using MyCal.ApiService.Common.Enum;

namespace MyCal.ApiService.Common.Model;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public double HeightInCm { get; set; }
    public double WeightInKg { get; set; }
    public double WeightGoal { get; set; }
    public double MaintenanceCalories { get; set; }
    public int Age { get; set; }
    public Gender Gender { get; set; }
    public ActivityLevel ActivityLevel { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public double CalculateBMR() => Gender switch
    {
        Gender.Male => (10 * WeightInKg) + (6.25 * HeightInCm) - (5 * Age) + 5,
        Gender.Female => (10 * WeightInKg) + (6.25 * HeightInCm) - (5 * Age) - 161,
        _ => throw new InvalidOperationException("Invalid gender value.")
    };

    public double CalculateMaintenanceCalories() => CalculateBMR() * ActivityLevel switch // Assuming ActivityLevel is a property of User
    {
        ActivityLevel.Sedentary => 1.2,
        ActivityLevel.LightlyActive => 1.375,
        ActivityLevel.ModeratelyActive => 1.55,
        ActivityLevel.VeryActive => 1.725,
        ActivityLevel.ExtraActive => 1.9,
        _ => throw new InvalidOperationException("Invalid activity level value.")
    };
}