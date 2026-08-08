using Microsoft.EntityFrameworkCore;
using MyCal.ApiService.Abstractions;
using MyCal.ApiService.Data;

namespace MyCal.ApiService.Features.Users.GetUsers;

public sealed class GetUsersHandler(AppDbContext context)
    : IQueryHandler<GetUsersQuery, List<UserResponse>>
{
    public async Task<List<UserResponse>> HandleAsync(
        GetUsersQuery query,
        CancellationToken cancellationToken) =>
        await context.Users
            .AsNoTracking()
            .Select(user => new UserResponse(
                user.Id, 
                user.FoodLogs,
                user.Name, 
                user.Email, 
                user.HeightInCm,
                user.WeightInKg,
                user.WeightGoal,
                user.GoalType,
                user.MaintenanceCalories, 
                user.Age, 
                user.Gender, 
                user.ActivityLevel, 
                user.OnboardingStatus))
            .ToListAsync(cancellationToken);
}
