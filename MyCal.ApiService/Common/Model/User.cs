using System.ComponentModel.DataAnnotations;
using MyCal.ApiService.Abstractions;
using MyCal.ApiService.Common.Enum;

namespace MyCal.ApiService.Common.Model;

public class User : DomainEntity
{
    public required string IdentityUserId { get; init; }
    public List<FoodLog> FoodLogs { get; set; } = [];
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public double HeightInCm { get; set; }
    public double WeightInKg { get; set; }
    public double WeightGoal { get; set; }
    public GoalType GoalType { get; set; }
    public double MaintenanceCalories { get; set; }
    public int Age { get; set; }
    public Gender Gender { get; set; }
    public ActivityLevel ActivityLevel { get; set; }
    public OnboardingStatus OnboardingStatus { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}