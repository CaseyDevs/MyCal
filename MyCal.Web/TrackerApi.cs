using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using MyCal.Application.Tracking;
using MyCal.Domain.Entity;
using MyCal.Shared.Tracking;

namespace MyCal.Web;

public static class TrackerApi
{
    public static void MapTrackerApi(this WebApplication app)
    {
        var api = app.MapGroup("/api").RequireAuthorization();
        api.AddEndpointFilter(async (context, next) =>
        {
            try
            {
                if (!HttpMethods.IsGet(context.HttpContext.Request.Method))
                    await context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context.HttpContext);
                return await next(context);
            }
            catch (AntiforgeryValidationException) { return Results.BadRequest(new { message = "Reload the page before saving." }); }
            catch (ProfileRequiredException) { return Results.NotFound(new { message = "Set up your profile first." }); }
            catch (KeyNotFoundException) { return Results.NotFound(new { message = "Record not found." }); }
            catch (ValidationException e) { return Results.BadRequest(new { message = e.Message }); }
        });
        api.MapGet("/antiforgery", (HttpContext context, IAntiforgery antiforgery) =>
            Results.Ok(new
            {
                token = antiforgery.GetAndStoreTokens(context).RequestToken
            }));
        api.MapGet("/profile", async (ClaimsPrincipal user, TrackerService service, CancellationToken ct) =>
            Results.Ok(await service.ProfileAsync(Id(user), ct)));
        api.MapPut("/profile", async (ProfileInput input, ClaimsPrincipal user, TrackerService service, CancellationToken ct) =>
            Results.Ok(await service.SaveProfileAsync(Id(user), user.FindFirstValue(ClaimTypes.Email) ?? "", input, ct)));
        api.MapGet("/diary/{date}", async (DateOnly date, ClaimsPrincipal user, TrackerService service, CancellationToken ct) =>
            Results.Ok(await service.DiaryAsync(Id(user), date, ct)));
        api.MapGet("/history", async (DateOnly from, DateOnly to, ClaimsPrincipal user, TrackerService service, CancellationToken ct) =>
            Results.Ok(await service.HistoryAsync(Id(user), from, to, ct)));
        api.MapPost("/diary/{date}/entries", async (DateOnly date, AddEntry input, ClaimsPrincipal user, TrackerService service, CancellationToken ct) =>
            Results.Ok(new
            {
                id = await service.AddEntryAsync(Id(user), date, input.Meal, input.Amount, input.Food, ct)
            }));
        api.MapPut("/entries/{id:int}", async (int id, EditEntry input, ClaimsPrincipal user, TrackerService service, CancellationToken ct) =>
        {
            await service.UpdateEntryAsync(Id(user), id, input.Meal, input.Amount, ct);
            return Results.NoContent();
        });
        api.MapDelete("/entries/{id:int}", async (int id, ClaimsPrincipal user, TrackerService service, CancellationToken ct) =>
        {
            await service.DeleteEntryAsync(Id(user), id, ct);
            return Results.NoContent();
        });
        api.MapPut("/diary/{date}/status", async (DateOnly date, SetStatus input, ClaimsPrincipal user, TrackerService service, CancellationToken ct) =>
        {
            await service.SetStatusAsync(Id(user), date, input.Status, ct);
            return Results.NoContent();
        });
        api.MapPost("/diary/{date}/copy", async (DateOnly date, CopyDay input, ClaimsPrincipal user, TrackerService service, CancellationToken ct) =>
        {
            await service.CopyAsync(Id(user), date, input.Destination, input.Meal, ct);
            return Results.NoContent();
        });
        api.MapGet("/foods", async (string search, IFoodSearch foods, CancellationToken ct) => Results.Ok(await foods.SearchAsync(search, ct)));
        api.MapGet("/foods/barcode/{barcode}", async (string barcode, IFoodSearch foods, CancellationToken ct) =>
            await foods.BarcodeAsync(barcode, ct) is { } food ? Results.Ok(food) : Results.NotFound());
        api.MapGet("/foods/saved", async (ClaimsPrincipal user, TrackerService service, CancellationToken ct) => Results.Ok(await service.SavedFoodsAsync(Id(user), ct: ct)));
        api.MapPost("/foods/saved", async (SaveFood input, ClaimsPrincipal user, TrackerService service, CancellationToken ct) =>
        {
            await service.SaveFoodAsync(Id(user), input.Food, input.Favourite, ct);
            return Results.NoContent();
        });
        api.MapGet("/targets", async (ClaimsPrincipal user, TrackerService service, CancellationToken ct) => Results.Ok(await service.TargetsAsync(Id(user), ct)));
        api.MapPut("/targets", async (TargetInput input, ClaimsPrincipal user, TrackerService service, CancellationToken ct) =>
        {
            await service.SaveTargetAsync(Id(user), input, ct);
            return Results.NoContent();
        });
        api.MapGet("/progress", async (DateOnly through, ClaimsPrincipal user, TrackerService service, CancellationToken ct) => Results.Ok(await service.ProgressAsync(Id(user), through, ct)));
        api.MapPut("/weights/{date}", async (DateOnly date, SetWeight input, ClaimsPrincipal user, TrackerService service, CancellationToken ct) =>
        {
            await service.SaveWeightAsync(Id(user), date, input.Kilograms, ct);
            return Results.NoContent();
        });
        api.MapDelete("/weights/{date}", async (DateOnly date, ClaimsPrincipal user, TrackerService service, CancellationToken ct) =>
        {
            await service.DeleteWeightAsync(Id(user), date, ct);
            return Results.NoContent();
        });
        api.MapPost("/reviews", async (ClaimsPrincipal user, TrackerService service, CancellationToken ct) => Results.Ok(await service.ReviewAsync(Id(user), ct)));
        api.MapGet("/reviews", async (ClaimsPrincipal user, TrackerService service, CancellationToken ct) => Results.Ok(await service.ReviewHistoryAsync(Id(user), ct)));
        api.MapPost("/reviews/{id:int}/accept", async (int id, ClaimsPrincipal user, TrackerService service, CancellationToken ct) =>
        {
            await service.AcceptReviewAsync(Id(user), id, ct);
            return Results.NoContent();
        });
    }
    private static string Id(ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();
    public sealed record AddEntry(Meal Meal, decimal Amount, FoodItem Food);
    public sealed record EditEntry(Meal Meal, decimal Amount);
    public sealed record SetStatus(DiaryStatus Status);
    public sealed record CopyDay(DateOnly Destination, Meal? Meal);
    public sealed record SetWeight(decimal Kilograms);
    public sealed record SaveFood(FoodItem Food, bool Favourite);
}
