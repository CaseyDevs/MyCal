using Microsoft.EntityFrameworkCore;
using MyCal.Domain.Abstractions;
using MyCal.Application.Data;

namespace MyCal.Application.Features.Users.GetUserById;

public sealed class GetUserByIdHandler(AppDbContext context)
    : IQueryHandler<GetUserByIdQuery, UserResponse?>
{
    public async Task<UserResponse?> HandleAsync(
        GetUserByIdQuery request,
        CancellationToken cancellationToken) =>
        await context.Users
            .AsNoTracking()
            .Where(user => user.Id == request.Id)
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
            .SingleOrDefaultAsync(cancellationToken);
}
