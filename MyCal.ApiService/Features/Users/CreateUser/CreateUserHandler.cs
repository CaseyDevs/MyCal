using Microsoft.EntityFrameworkCore;
using MyCal.ApiService.Abstractions;
using MyCal.ApiService.Common.Model;
using MyCal.ApiService.Common.Result;
using MyCal.ApiService.Data;

namespace MyCal.ApiService.Features.Users.CreateUser;

public sealed class CreateUserHandler(AppDbContext context)
    : ICommandHandler<CreateUserCommand, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> HandleAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = command.Email.Trim().ToLowerInvariant();
        var emailExists = await context.Users
            .AsNoTracking()
            .AnyAsync(user => user.Email == normalizedEmail, cancellationToken);

        if (emailExists)
        {
            return Result<UserResponse>.Fail(
                "EmailAlreadyExists",
                "An account with this email already exists.");;
        }

        var user = new User
        {
            Name = command.Name.Trim(),
            Email = normalizedEmail,
            HeightInCm = command.HeightInCm,
            WeightInKg = command.WeightInKg,
            WeightGoal = command.WeightGoal,
            Age = command.Age,
            Gender = command.Gender,
            ActivityLevel = command.ActivityLevel
        };

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        var result = new UserResponse(
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
            user.CreatedAt);

        return Result<UserResponse>.Success(result);
    }
}