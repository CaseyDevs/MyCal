using Microsoft.EntityFrameworkCore;
using MyCal.ApiService.Abstractions;
using MyCal.ApiService.Common.Result;
using MyCal.ApiService.Data;

namespace MyCal.ApiService.Features.Users.UpdateUser;

public sealed class UpdateUserCommandHandler(
    AppDbContext context
) : ICommandHandler<
        UpdateUserCommand, 
        Result<UserResponse>>
{
    public async Task<Result<UserResponse>> HandleAsync(
        UpdateUserCommand command, 
        CancellationToken cancellationToken)
    {
        var user = await context.Users
            .SingleOrDefaultAsync(
                user => user.Id == command.Id,
                cancellationToken);

        if (user is null)
        {
            return Result<UserResponse>.Fail(
                "User does not exist.",
                "UserNotFound");
        }

        // update existing user
        user.Name = command.Name.Trim();
        user.HeightInCm = command.HeightInCm;
        user.WeightInKg = command.WeightInKg;
        user.WeightGoal = command.WeightGoal;
        user.Age = command.Age;
        user.Gender = command.Gender;
        user.ActivityLevel = command.ActivityLevel;

        await context.SaveChangesAsync(cancellationToken);
        
        var response = new UserResponse(
            Id: user.Id,
            Name: user.Name,
            Email: user.Email,
            HeightInCm: user.HeightInCm,
            WeightInKg: user.WeightInKg,
            WeightGoal: user.WeightGoal,
            Age: user.Age,
            Gender: user.Gender,
            ActivityLevel: user.ActivityLevel,
            UpdatedAt: DateTime.UtcNow,
            CreatedAt: user.CreatedAt);

        return Result<UserResponse>.Success(response);
    }
}