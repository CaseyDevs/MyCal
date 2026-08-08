using Microsoft.EntityFrameworkCore;
using MyCal.ApiService.Abstractions;
using MyCal.ApiService.Calculators;
using MyCal.ApiService.Common.Enum;
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
        var profileExists = await context.Users
            .AsNoTracking()
            .AnyAsync(user => user.IdentityUserId == command.IdentityUserId, cancellationToken);

        if (profileExists)
        {
            return Result<UserResponse>.Fail(
                "ProfileAlreadyExists",
                "An account already exists.");;
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
