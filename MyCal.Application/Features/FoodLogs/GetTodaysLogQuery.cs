using Microsoft.EntityFrameworkCore;
using MyCal.Application.Common.Result;
using MyCal.Application.Data;
using MyCal.Domain.Abstractions;
using MyCal.Domain.Entity;
using MyCal.Shared.Dto.FoodLog;

namespace MyCal.Application.Features.FoodLogs;

public sealed record GetTodaysLogQuery(
    int UserId);

internal sealed class GetTodaysLogQueryHandler(AppDbContext context)
    : IQueryHandler<GetTodaysLogQuery, Result<FoodLogResult>>
{
    public async Task<Result<FoodLogResult>> HandleAsync(
        GetTodaysLogQuery query,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var todaysLog = await context.FoodLogs
            .Include(log => log.Entries)
            .ThenInclude(entry => entry.Food)
            .SingleOrDefaultAsync(log => log.Date == today && log.UserId == query.UserId, cancellationToken);

        if (todaysLog is null)
        {
            var userExists = await context.Users.AnyAsync(
                user => user.Id == query.UserId,
                cancellationToken);

            if (!userExists)
            {
                return Result<FoodLogResult>.Fail(
                    "User does not exist.",
                    "UserNotFound");
            }

            todaysLog = FoodLogHelpers.CreateFoodLog(query.UserId, today);

            context.FoodLogs.Add(todaysLog);
            await context.SaveChangesAsync(cancellationToken);
        }

        var entries = FoodLogHelpers.MapFoodLogEntries(todaysLog);

        return Result<FoodLogResult>.Success(
            new FoodLogResult(
                todaysLog.Id,
                todaysLog.Date,
                entries,
                todaysLog.CalculateTotalCalories(),
                todaysLog.CalculateTotalProtein(),
                todaysLog.CalculateTotalCarbohydrates(),
                todaysLog.CalculateTotalFats()));
    }
}
