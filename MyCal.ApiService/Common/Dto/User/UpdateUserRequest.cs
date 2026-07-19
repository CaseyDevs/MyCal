using MyCal.ApiService.Common.Enum;

namespace MyCal.ApiService.Common.Dto.User;

public sealed record UpdateUserRequest(
    string Name,
    double HeightInCm,
    double WeightInKg,
    double WeightGoal,
    int Age,
    Gender Gender,
    ActivityLevel ActivityLevel);