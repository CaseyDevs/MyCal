using MyCal.Domain.Enum;

namespace MyCal.Application.Features.Users.UpdateUser;

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