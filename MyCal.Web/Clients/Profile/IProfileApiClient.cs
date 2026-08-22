using MyCal.Domain.Entity;
using MyCal.Shared.Dto.FoodLog;

namespace MyCal.Web.Clients.Profile;

public interface IProfileApiClient 
{
    Task<UserProfileResponse?> GetUserProfileAsync(
        CancellationToken cancellationToken);

    Task<UserProfileResponse> CreateUserProfileAsync(
        UserProfileRequest profile,
        CancellationToken cancellationToken);
}

public sealed record UserProfileRequest(
    string Name,
    string Email,
    double HeightInCm,
    double WeightInKg,
    double WeightGoal,
    int Age,
    Gender Gender,
    ActivityLevel ActivityLevel
);

public sealed record UserProfileResponse(
    int Id,
    List<FoodLogResult> FoodLogs,
    string Name,
    string Email,
    double HeightInCm,
    double WeightInKg,
    double WeightGoal,
    double MaintenanceCalories,
    int Age,
    Gender Gender,
    ActivityLevel ActivityLevel,
    OnboardingStatus OnboardingStatus,
    DateTime? UpdatedAt,
    DateTime CreatedAt
);

public enum Gender
{
    Male = 1,
    Female = 2
}

public enum ActivityLevel
{
    Sedentary = 1,
    LightlyActive = 2,
    ModeratelyActive = 3,
    VeryActive = 4,
    ExtraActive = 5
}

public enum OnboardingStatus
{
    Pending = 1,
    Complete = 2
}
