using Microsoft.EntityFrameworkCore;
using MyCal.ApiService.Abstractions;
using MyCal.ApiService.Data;

namespace MyCal.ApiService.Features.Users.GetUserById;

public sealed class GetUserByIdHandler(AppDbContext context)
    : IQueryHandler<GetUserByIdQuery, UserResponseDto?>
{
    public Task<UserResponseDto?> HandleAsync(
        GetUserByIdQuery request,
        CancellationToken cancellationToken) =>
        context.Users
            .AsNoTracking()
            .Where(user => user.Id == request.Id)
            .Select(user => new UserResponseDto(
                user.Id, 
                user.Name, 
                user.Email, 
                user.HeightInCm, 
                user.WeightInKg,
                user.WeightGoal, 
                user.Age, 
                user.Gender, 
                user.ActivityLevel, 
                user.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);
}
