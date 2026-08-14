using Microsoft.EntityFrameworkCore;
using MyCal.Domain.Abstractions;
using MyCal.Application.Calculators;
using MyCal.Domain.Enum;
using MyCal.Domain.Entity;
using MyCal.Application.Common.Result;
using MyCal.Application.Data;

namespace MyCal.Application.Features.Users.CreateUser;

public sealed class CreateUserHandler(AppDbContext context)
    : ICommandHandler<CreateUserCommand, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> HandleAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        var profileExists = await context.Users
            .AsNoTracking()
            .AnyAsync(user => user.IdentityUserId == command.IdentityUserId, cancellationToken);

        if (profileExists)
        {
            return Result<UserResponse>.Fail(
                "An account with this email already exists.",
                "ProfileAlreadyExists");;
        }

        var user = new User
        {
            IdentityUserId = command.IdentityUserId,
            Name = command.Name.Trim(),
            Email = command.Email.Trim(),
            HeightInCm = command.HeightInCm,
            WeightInKg = command.WeightInKg,
            WeightGoal = command.WeightGoal,
            Age = command.Age,
            Gender = command.Gender,
            ActivityLevel = command.ActivityLevel,
            OnboardingStatus = OnboardingStatus.Complete
        };

        user.GoalType = CalorieCalculator.DetermineGoalType(user);
        user.MaintenanceCalories = CalorieCalculator.CalculateMaintenanceCalories(user);
        
        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        var result = new UserResponse(
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
            user.OnboardingStatus);

        return Result<UserResponse>.Success(result);
    }
}
