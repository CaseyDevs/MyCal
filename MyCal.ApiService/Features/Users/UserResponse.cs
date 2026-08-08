using MyCal.ApiService.Common.Enum;
using MyCal.ApiService.Common.Model;

namespace MyCal.ApiService.Features.Users;

public sealed record UserResponse(
    int Id,
    List<FoodLog> FoodLogs,
    string Name,
    string Email,
    double HeightInCm,
    double WeightInKg,
    double WeightGoal,
    GoalType GoalType,
    double MaintenanceCalories,
    int Age,
    Gender Gender,
    ActivityLevel ActivityLevel,
    OnboardingStatus OnboardingStatus);
