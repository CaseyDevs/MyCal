using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MyCal.Application.Common.Result;
using MyCal.Application.Data;
using MyCal.Domain.Abstractions;
using MyCal.Domain.Entity;

namespace MyCal.Application.Features.Foods;

public sealed record AddToFoodLogCommand(
    int UserId,
    string Name,
    string? Brand,
    int QuantityInGrams,
    double CaloriesPer100G,
    double ProteinPer100G,
    double CarbohydratesPer100G,
    double FatsPer100G);
    
internal sealed class AddToFoodLogCommandHandler(
    AppDbContext context) 
    :ICommandHandler<AddToFoodLogCommand, Result>
{
    public async Task<Result> HandleAsync(
        AddToFoodLogCommand command, 
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        
        var foodLog = await context.FoodLogs
            .SingleOrDefaultAsync(log => log.UserId == command.UserId && log.Date == today, cancellationToken);

        if (foodLog is null)
        {
            var userExists = await context.Users.AnyAsync(
                user => user.Id == command.UserId,
                cancellationToken);

            if (!userExists)
            {
                return Result.Fail("User does not exist.");
            }

            foodLog = FoodLogHelpers.CreateFoodLog(command.UserId, today);
            context.FoodLogs.Add(foodLog);
        }
        
        var food = await context.Foods
            .Where(f => f.Name == command.Name
                && f.Brand == command.Brand)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (food is null)
        {
            food = new Food
            {
                Name = command.Name,
                Brand = command.Brand,
                Calories = command.CaloriesPer100G,
                Protein = command.ProteinPer100G,
                Carbohydrates = command.CarbohydratesPer100G,
                Fats = command.FatsPer100G
            };
            
            context.Foods.Add(food);
        }
        
        var entry = new FoodLogEntry
        {
            Food = food,
            FoodLog = foodLog,
            QuantityInGrams = command.QuantityInGrams
        };
        
        context.FoodLogEntries.Add(entry);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(null);
    }
}

public sealed class AddToFoodLogCommandValidator
    : AbstractValidator<AddToFoodLogCommand>
{
    public AddToFoodLogCommandValidator()
    {
        RuleFor(command => command.UserId).GreaterThan(0);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Brand).MaximumLength(200);
        RuleFor(command => command.QuantityInGrams).GreaterThan(0);
        RuleFor(command => command.CaloriesPer100G).GreaterThanOrEqualTo(0);
        RuleFor(command => command.ProteinPer100G).GreaterThanOrEqualTo(0);
        RuleFor(command => command.CarbohydratesPer100G).GreaterThanOrEqualTo(0);
        RuleFor(command => command.FatsPer100G).GreaterThanOrEqualTo(0);
    }
}
