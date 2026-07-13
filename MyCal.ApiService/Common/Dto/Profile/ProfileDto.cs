using MyCal.ApiService.Common.Enum;

namespace MyCal.ApiService.Common.Dto.Profile;

public sealed record ProfileRequestDto(
        string Name,
        string Email,
        double HeightInCm,
        double WeightInKg,
        double WeightGoal,
        int Age,
        Gender Gender,
        ActivityLevel ActivityLevel
    );

public sealed record ProfileResponseDto(
    int Id,
    string Name,
    string Email,
    double HeightInCm,
    double WeightInKg,
    double WeightGoal,
    int Age,
    Gender Gender,
    ActivityLevel ActivityLevel,
    DateTime CreatedAt
);