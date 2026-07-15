using Microsoft.EntityFrameworkCore;
using MyCal.ApiService.Common.Model;
using MyCal.ApiService.Data;

namespace MyCal.ApiService.Features.Users.CreateUser;

public sealed class CreateUserHandler(AppDbContext context)
{
    public async Task<CreateUserResult> HandleAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var emailExists = await context.Users
            .AsNoTracking()
            .AnyAsync(user => user.Email == normalizedEmail, cancellationToken);

        if (emailExists)
        {
            return CreateUserResult.DuplicateEmail();
        }

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = normalizedEmail,
            HeightInCm = request.HeightInCm,
            WeightInKg = request.WeightInKg,
            WeightGoal = request.WeightGoal,
            Age = request.Age,
            Gender = request.Gender,
            ActivityLevel = request.ActivityLevel
        };

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        var result = new UserResponseDto(
            user.Id, 
            user.Name, 
            user.Email, 
            user.HeightInCm, 
            user.WeightInKg,
            user.WeightGoal, 
            user.Age, 
            user.Gender, 
            user.ActivityLevel, 
            user.CreatedAt);

        return CreateUserResult.Success(result);
    }
}
