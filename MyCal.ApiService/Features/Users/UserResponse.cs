using MyCal.ApiService.Common.Enum;

namespace MyCal.ApiService.Features.Users;

public sealed record UserResponse(
    int Id,
    string Name,
    string Email,
    double HeightInCm,
    double WeightInKg,
    double WeightGoal,
    int Age,
    Gender Gender,
    ActivityLevel ActivityLevel,
    DateTime? UpdatedAt,
    DateTime CreatedAt);
