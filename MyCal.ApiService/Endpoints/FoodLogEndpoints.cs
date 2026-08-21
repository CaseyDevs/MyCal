using Microsoft.AspNetCore.Mvc;
using MyCal.Application.Common.Result;
using MyCal.Application.Features.FoodLogs;
using MyCal.Application.Features.Foods;
using MyCal.Domain.Abstractions;
using MyCal.Domain.Entity;
using MyCal.Shared.Dto;

namespace MyCal.ApiService.Endpoints;

public static class FoodLogEndpoints
{
    public static WebApplication MapFoodLogEndpoints(this WebApplication app)
    {
        var foodLogs = app.MapGroup("/food-logs");

        foodLogs.MapGet("/{id:int}", async (
                [FromRoute] int id,
                [FromServices] IQueryHandler<GetFoodLogQuery, Result<FoodLog>> handler,
                CancellationToken cancellationToken) =>
            await handler.HandleAsync(new GetFoodLogQuery(id), cancellationToken));

        foodLogs.MapPost("/", async (
            [FromBody] FoodLogRequest request,
            [FromServices] ICommandHandler<AddToFoodLogCommand, Result> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new AddToFoodLogCommand(
                FoodLogId: request.FoodLogId,
                Name: request.Name,
                Brand: request.Brand,
                QuantityInGrams: request.QuantityInGrams,
                CaloriesPer100G: request.CaloriesPer100G,
                ProteinPer100G: request.ProteinPer100G,
                CarbohydratesPer100G: request.CarbohydratesPer100G,
                FatsPer100G: request.FatsPer100G);

            var result = await handler.HandleAsync(command, cancellationToken);
            return result.IsSuccess 
                ? Results.NoContent()
                : Results.NotFound(result.ErrorMessage);
        });

        return app;
    }
}