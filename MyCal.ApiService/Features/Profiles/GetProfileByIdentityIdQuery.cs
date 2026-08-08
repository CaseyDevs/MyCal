using Microsoft.EntityFrameworkCore;
using MyCal.ApiService.Abstractions;
using MyCal.ApiService.Data;
using MyCal.ApiService.Features.Users;

namespace MyCal.ApiService.Features.Profiles;

public sealed record GetProfileByIdentityIdQuery(
    string IdentityUserId
);

public sealed class GetProfileByIdentityIdQueryHandler(
    AppDbContext context
) : IQueryHandler<GetProfileByIdentityIdQuery, UserResponse?>
{
    public async Task<UserResponse?> HandleAsync(
        GetProfileByIdentityIdQuery query,
        CancellationToken cancellationToken) =>
        await context.Users
            .AsNoTracking()
            .Where(user => user.IdentityUserId == query.IdentityUserId)
            .Select(profile => new UserResponse(
                profile.Id,
                profile.FoodLogs,
                profile.Name,
                profile.Email,
                profile.HeightInCm,
                profile.WeightInKg,
                profile.WeightGoal,
                profile.GoalType,
                profile.MaintenanceCalories,
                profile.Age,
                profile.Gender,
                profile.ActivityLevel,
                profile.OnboardingStatus))
            .SingleOrDefaultAsync(cancellationToken);
}
