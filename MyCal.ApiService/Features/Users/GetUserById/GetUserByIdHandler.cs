using Microsoft.EntityFrameworkCore;
using MyCal.ApiService.Abstractions;
using MyCal.ApiService.Data;

namespace MyCal.ApiService.Features.Users.GetUserById;

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
                user.Name, 
                user.Email, 
                user.HeightInCm, 
                user.WeightInKg,
                user.WeightGoal, 
                user.Age, 
                user.Gender, 
                user.ActivityLevel, 
                null,
                user.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);
}
