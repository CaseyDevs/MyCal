using Microsoft.EntityFrameworkCore;
using MyCal.Application.Common.Result;
using MyCal.Application.Data;
using MyCal.Domain.Abstractions;
using MyCal.Domain.Entity;

namespace MyCal.Application.Features.FoodLogs;

public sealed record GetFoodLogQuery(
    int Id);

internal sealed class GetFoodLogQueryHandler(AppDbContext context)
    : IQueryHandler<GetFoodLogQuery, Result<FoodLog>>
{
    public async Task<Result<FoodLog>> HandleAsync(
        GetFoodLogQuery request,
        CancellationToken cancellationToken)
    {
        var log = await context.FoodLogs
            .Where(log => log.Id == request.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return log is null
            ? Result<FoodLog>.Fail(
                "Food log does not exist.",
                "FoodLogNotFound")
            : Result<FoodLog>.Success(log);
    }
}