using Microsoft.EntityFrameworkCore;
using MyCal.Domain.Abstractions;
using MyCal.Application.Calculators;
using MyCal.Application.Common.Result;
using MyCal.Application.Data;

namespace MyCal.Application.Features.Users.UpdateUser;

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
            .Include(user => user.FoodLogs)
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
        user.GoalType = CalorieCalculator.DetermineGoalType(user);
        user.MaintenanceCalories = CalorieCalculator.CalculateMaintenanceCalories(user);

        await context.SaveChangesAsync(cancellationToken);
        
        var response = new UserResponse(
            Id: user.Id,
            FoodLogs: user.FoodLogs,
            Name: user.Name,
            Email: user.Email,
            HeightInCm: user.HeightInCm,
            WeightInKg: user.WeightInKg,
            WeightGoal: user.WeightGoal,
            GoalType: user.GoalType,
            MaintenanceCalories: user.MaintenanceCalories,
            Age: user.Age,
            Gender: user.Gender,
            ActivityLevel: user.ActivityLevel,
            OnboardingStatus: user.OnboardingStatus);

        return Result<UserResponse>.Success(response);
    }
}