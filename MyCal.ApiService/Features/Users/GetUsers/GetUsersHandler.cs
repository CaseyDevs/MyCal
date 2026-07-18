using Microsoft.EntityFrameworkCore;
using MyCal.ApiService.Abstractions;
using MyCal.ApiService.Data;

namespace MyCal.ApiService.Features.Users.GetUsers;

public sealed class GetUsersHandler(AppDbContext context)
    : IQueryHandler<GetUsersQuery, List<UserResponseDto>>
{
    public async Task<List<UserResponseDto>> HandleAsync(
        GetUsersQuery query,
        CancellationToken cancellationToken) =>
        await context.Users
            .AsNoTracking()
            .Select(user => new UserResponseDto(
                user.Id, user.Name, user.Email, user.HeightInCm, user.WeightInKg,
                user.WeightGoal, user.Age, user.Gender, user.ActivityLevel, user.CreatedAt))
            .ToListAsync(cancellationToken);
}
