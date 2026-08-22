using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MyCal.Application.Common.Result;
using MyCal.Application.Features.FoodLogs;
using MyCal.Application.Features.Foods;
using MyCal.Domain.Abstractions;
using MyCal.Shared.Dto;
using MyCal.Shared.Dto.FoodLog;

namespace MyCal.ApiService.Endpoints;

public static class FoodLogEndpoints
{
    public static WebApplication MapFoodLogEndpoints(this WebApplication app)
    {
        var foodLogs = app.MapGroup("/food-logs");

        foodLogs.MapGet("/{id:int}", async (
            [FromRoute] int id,
            [FromServices] IQueryHandler<GetFoodLogQuery, Result<FoodLogResult>> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(
                new GetFoodLogQuery(id),
                cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.NotFound(new { message = result.ErrorMessage });
        });

        foodLogs.MapGet("/today/{userId:int}", async (
            [FromRoute] int userId,
            [FromServices] IQueryHandler<GetTodaysLogQuery, Result<FoodLogResult>> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(
                new GetTodaysLogQuery(userId),
                cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.NotFound(new { message = result.ErrorMessage });
        });

        foodLogs.MapPost("/", async (
            [FromBody] FoodLogRequest request,
            [FromServices] IValidator<AddToFoodLogCommand> validator,
            [FromServices] ICommandHandler<AddToFoodLogCommand, Result> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new AddToFoodLogCommand(
                UserId: request.UserId,
                Name: request.Name,
                Brand: request.Brand,
                QuantityInGrams: request.QuantityInGrams,
                CaloriesPer100G: request.CaloriesPer100G,
                ProteinPer100G: request.ProteinPer100G,
                CarbohydratesPer100G: request.CarbohydratesPer100G,
                FatsPer100G: request.FatsPer100G);

            var validation = await validator.ValidateAsync(command, cancellationToken);

            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            var result = await handler.HandleAsync(command, cancellationToken);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.NotFound(new { message = result.ErrorMessage });
        });

        return app;
    }
}
