using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MyCal.Application.Calculators;
using MyCal.Domain.Entity;
using MyCal.Domain.Enum;
using MyCal.Shared.Tracking;
using MyCal.Web.Components.Tracking;
using System.Globalization;

namespace MyCal.Web.Components.Pages;

public partial class Home
{
    [Parameter]
    public string? DateText { get; set; }
    private DiaryDay? _day; private DateOnly _today, _copyTo;
    private IReadOnlyList<DaySummary> _week = [];
    private bool _showFood; private Meal _addMeal, _editMeal;
    private int? _editing; private decimal _editAmount; private string _copyMeal = "";
    protected override Task OnParametersSetAsync() => Reload();
    private Task Reload() => Run(async () => { if (!await RequireProfile()) return; _today = await Tracker.TodayAsync(IdentityId); var date = string.IsNullOrEmpty(DateText) ? _today : DateOnly.TryParseExact(DateText, "yyyy-MM-dd", out var parsed) ? parsed : throw new System.ComponentModel.DataAnnotations.ValidationException("Choose a valid diary date."); if (date > _today) throw new System.ComponentModel.DataAnnotations.ValidationException("Future diary dates aren't available."); _day = await Tracker.DiaryAsync(IdentityId, date); _week = await Tracker.HistoryAsync(IdentityId, _today.AddDays(-6), _today); _copyTo = _today; });
    private async Task Refresh()
    {
        _day = await Tracker.DiaryAsync(IdentityId, _day!.Date);
        _week = await Tracker.HistoryAsync(IdentityId, _today.AddDays(-6), _today);
    }
    private void Go(DateOnly date)
    {
        _editing = null;
        Notice = null;
        Navigation.NavigateTo(date == _today ? "/" : $"/diary/{date:yyyy-MM-dd}");
    }
    private void ChangeDate(ChangeEventArgs args)
    {
        if (DateOnly.TryParse(args.Value?.ToString(), out var date))
            Go(date);
    }
    private void OpenFood(Meal meal)
    {
        _addMeal = meal;
        _showFood = true;
    }
    private async Task AddFood(FoodComposer.FoodSelection selection)
    {
        await Tracker.AddEntryAsync(await Account.IdAsync(), _day!.Date, selection.Meal, selection.Amount, selection.Food);
        await Refresh();
        _showFood = false;
    }
    private void Edit(DiaryEntry entry)
    {
        _editing = entry.Id;
        _editAmount = entry.Amount;
        _editMeal = entry.Meal;
    }
    private Task SaveEdit() => Run(async () => { await Tracker.UpdateEntryAsync(IdentityId, _editing!.Value, _editMeal, _editAmount); _editing = null; await Refresh(); });
    private Task Delete(int id) => Run(async () => { await Tracker.DeleteEntryAsync(IdentityId, id); _editing = null; await Refresh(); });
    private Task StatusChanged(ChangeEventArgs args) => Run(async () => { if (Enum.TryParse<DiaryStatus>(args.Value?.ToString(), out var status)) await Tracker.SetStatusAsync(IdentityId, _day!.Date, status); await Refresh(); });
    private Task Copy() => Run(async () => { await Tracker.CopyAsync(IdentityId, _day!.Date, _copyTo, string.IsNullOrEmpty(_copyMeal) ? null : Enum.Parse<Meal>(_copyMeal)); Notice = $"Entries copied to {_copyTo:d MMMM}."; await Refresh(); });
    private string RingStyle => $"--ring:{Math.Clamp((_day?.Target?.Calories ?? 0) > 0 ? _day!.Totals.Calories / _day.Target!.Calories * 100 : 0, 0, 100).ToString("0.###", CultureInfo.InvariantCulture)}%";
    private string BarStyle(DaySummary d) => $"height:{Math.Clamp(d.Calories / Math.Max(_week.Max(x => Math.Max(x.Calories, x.Target?.Calories ?? 0)), 1) * 100, 2, 100).ToString("0.#", CultureInfo.InvariantCulture)}%";
    private static string Macro(decimal? source, decimal value) => source is null ? "?" : value.ToString("0.#") + " g";
    private static string MealSymbol(Meal m) => m switch { Meal.Breakfast => "☀", Meal.Lunch => "◒", Meal.Dinner => "☾", _ => "✧" };
}
