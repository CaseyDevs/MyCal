using Microsoft.EntityFrameworkCore;
using MyCal.ApiService.Data;

namespace MyCal.ApiService.Features.Users.GetUsers;

public sealed class GetUsersHandler(AppDbContext context)
{
    public Task<List<UserResponseDto>> HandleAsync(CancellationToken cancellationToken) =>
        context.Users
            .AsNoTracking()
            .Select(user => new UserResponseDto(
                user.Id, user.Name, user.Email, user.HeightInCm, user.WeightInKg,
                user.WeightGoal, user.Age, user.Gender, user.ActivityLevel, user.CreatedAt))
            .ToListAsync(cancellationToken);
}
