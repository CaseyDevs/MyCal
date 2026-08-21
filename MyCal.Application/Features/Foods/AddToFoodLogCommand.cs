using Microsoft.EntityFrameworkCore;
using MyCal.Application.Common.Result;
using MyCal.Application.Data;
using MyCal.Domain.Abstractions;
using MyCal.Domain.Entity;

namespace MyCal.Application.Features.Foods;

public sealed record AddToFoodLogCommand(
    int FoodLogId,
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
        
        var foodLog = await context.FoodLogs
            .Where(fl => fl.Id == command.FoodLogId)
            .FirstOrDefaultAsync(cancellationToken);

        if (foodLog is null)
        {
            return Result.Fail("Food log does not exist.");
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
