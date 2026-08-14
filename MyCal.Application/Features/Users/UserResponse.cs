using MyCal.Domain.Enum;
using MyCal.Domain.Entity;

namespace MyCal.Application.Features.Users;

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
