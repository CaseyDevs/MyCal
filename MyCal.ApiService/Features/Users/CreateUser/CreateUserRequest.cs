using MyCal.ApiService.Common.Enum;

namespace MyCal.ApiService.Features.Users.CreateUser;

// This is the command: it describes the data needed to create a user.
public sealed record CreateUserRequest(
    string Name,
    string Email,
    double HeightInCm,
    double WeightInKg,
    double WeightGoal,
    int Age,
    Gender Gender,
    ActivityLevel ActivityLevel);
