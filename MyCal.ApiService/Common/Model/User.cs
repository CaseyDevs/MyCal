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
    public double MaintenanceCalories { get; }
    public int Age { get; set; }
    public Gender Gender { get; set; }
    public ActivityLevel ActivityLevel { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}