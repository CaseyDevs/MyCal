using MyCal.ApiService.Common.Enum;

namespace MyCal.ApiService.Features.Users.UpdateUser;

public sealed record UpdateUserCommand(
    int Id,
    string Name,
    double HeightInCm,
    double WeightInKg,
    double WeightGoal,
    int Age,
    Gender Gender,
    ActivityLevel ActivityLevel
);