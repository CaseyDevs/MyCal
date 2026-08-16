using System.Reflection.Metadata.Ecma335;
using Microsoft.AspNetCore.Mvc;
using MyCal.Domain.Abstractions;
using MyCal.Application.Features.Foods;
using MyCal.Shared.Dto.Food;

namespace MyCal.ApiService.Endpoints;

public static class FoodEndpoints
{
    public static WebApplication MapFoodEndpoints(
        this WebApplication app
    )
    {
        var food = app.MapGroup("/foods");

        food.MapGet("/", async(
            [FromQuery] string search,
            [FromServices] IQueryHandler<SearchFoodByTextQuery, IReadOnlyList<FoodSearchResult>> handler,
            CancellationToken cancellationToken) => 
            {
                var results = await handler.HandleAsync(
                    new SearchFoodByTextQuery(search),
                    cancellationToken);

                return Results.Ok(results);
            });

        return app;
    }

}