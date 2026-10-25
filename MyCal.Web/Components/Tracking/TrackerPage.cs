using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using MyCal.Application.Tracking;
using MyCal.Shared.Tracking;
using MyCal.Web.Security;

namespace MyCal.Web.Components.Tracking;

public abstract class TrackerPage : ComponentBase
{
    [Inject] protected TrackerService Tracker { get; set; } = null!;
    [Inject] protected CurrentAccount Account { get; set; } = null!;
    [Inject] protected NavigationManager Navigation { get; set; } = null!;
    [Inject] protected ILogger<TrackerPage> Logger { get; set; } = null!;
    protected string IdentityId = "";
    protected Profile? UserProfile;
    protected bool Busy;
    protected string? Error;
    protected string? Notice;
    protected async Task Run(Func<Task> action)
    {
        if (Busy)
            return;
        Busy = true;
        Error = null;
        try
        {
            IdentityId = await Account.IdAsync();
            await action();
        }
        catch (ProfileRequiredException) { Navigation.NavigateTo("/settings"); }
        catch (UnauthorizedAccessException) { Navigation.NavigateTo("/account/login", forceLoad: true); }
        catch (Exception e) when (e is ValidationException or KeyNotFoundException) { Error = e.Message; }
        catch (Exception e) when (e is DbUpdateException or Npgsql.NpgsqlException or HttpRequestException or TaskCanceledException)
        {
            Logger.LogWarning("Tracking operation failed ({FailureType}).", e.GetType().Name);
            Error = "We couldn't save or load that change. Check your connection and try again.";
        }
        finally { Busy = false; }
    }
    protected async Task<bool> RequireProfile()
    {
        UserProfile = await Tracker.ProfileAsync(IdentityId);
        if (UserProfile is not null)
            return true;
        Navigation.NavigateTo("/settings");
        return false;
    }
    protected static string Number(decimal value) => value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
    protected static string Unit(MyCal.Domain.Entity.NutritionUnit unit) => unit switch
    {
        MyCal.Domain.Entity.NutritionUnit.Grams => "g",
        MyCal.Domain.Entity.NutritionUnit.Millilitres => "ml",
        _ => "servings"
    };
}
