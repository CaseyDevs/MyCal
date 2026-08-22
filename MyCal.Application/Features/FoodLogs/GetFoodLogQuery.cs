using Microsoft.EntityFrameworkCore;
using MyCal.Application.Common.Result;
using MyCal.Application.Data;
using MyCal.Domain.Abstractions;
using MyCal.Domain.Entity;
using MyCal.Shared.Dto.FoodLog;

namespace MyCal.Application.Features.FoodLogs;

public sealed record GetFoodLogQuery(
    int Id);

internal sealed class GetFoodLogQueryHandler(AppDbContext context)
    : IQueryHandler<GetFoodLogQuery, Result<FoodLogResult>>
{
    public async Task<Result<FoodLogResult>> HandleAsync(
        GetFoodLogQuery request,
        CancellationToken cancellationToken)
    {
        var log = await context.FoodLogs
            .AsNoTracking()
            .Include(log => log.Entries)
            .ThenInclude(entry => entry.Food)
            .SingleOrDefaultAsync(log => log.Id == request.Id, cancellationToken);

        if (log is null)
        {
            return Result<FoodLogResult>.Fail("Food log does not exist.", "FoodLogNotFound");
        }

        var entries = FoodLogHelpers.MapFoodLogEntries(log);

        var result = new FoodLogResult(
            Id: log.Id,
            Date: log.Date,
            Entries: entries,
            TotalCalories: log.CalculateTotalCalories(),
            TotalProtein: log.CalculateTotalProtein(),
            TotalCarbohydrates: log.CalculateTotalCarbohydrates(),
            TotalFats: log.CalculateTotalFats());

        return Result<FoodLogResult>.Success(result);
    }
}