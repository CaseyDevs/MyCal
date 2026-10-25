using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MyCal.Application.Calculators;
using MyCal.Domain.Entity;
using MyCal.Domain.Enum;
using MyCal.Shared.Tracking;
using MyCal.Web.Components.Tracking;
using System.Globalization;

namespace MyCal.Web.Components.Pages;

public partial class Settings
{
    private ProfileInput _input = new(); private TargetInput _target = new();
    private IReadOnlyList<Target> _revisions = []; private bool _admin; private string _theme = "system";
    protected override Task OnInitializedAsync() => Run(async () => { var account = await Account.GetAsync(); _admin = account.IsAdmin; UserProfile = await Tracker.ProfileAsync(IdentityId); if (UserProfile is { } p) { _input = new() { Name = p.Name, Age = p.Age, Height = p.Height, Weight = p.Weight, GoalWeight = p.GoalWeight, Coefficient = p.Coefficient, Activity = p.Activity, TimeZoneId = p.TimeZoneId }; await LoadTargets(); } });
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _theme = await JS.InvokeAsync<string>("mycalTheme.get");
            StateHasChanged();
        }
    }
    private async Task LoadTargets()
    {
        _revisions = await Tracker.TargetsAsync(IdentityId);
        if (_revisions.FirstOrDefault() is { } t)
            _target = new()
            {
                Calories = t.Calories,
                Protein = decimal.Round(t.Protein, 1),
                Carbohydrates = decimal.Round(t.Carbohydrates, 1),
                Fat = decimal.Round(t.Fat, 1),
                Goal = t.Goal,
                Training = t.Training,
                ProteinPreference = t.ProteinPreference,
                Diet = t.Diet
            };
    }
    private Task SaveProfile() => Run(async () => { var account = await Account.GetAsync(); UserProfile = await Tracker.SaveProfileAsync(IdentityId, account.Email, _input); await LoadTargets(); Notice = _revisions.Count == 0 ? "Profile saved. Set manual targets below to complete your plan." : "Profile saved. Your diary is ready."; });
    private Task SaveTargets() => Run(async () => { await Tracker.SaveTargetAsync(IdentityId, _target); await LoadTargets(); Notice = "Your new targets are scheduled for tomorrow. Earlier days keep their original targets."; });
    private Task RecalculateCarbs() => Run(() => { MarkCustom(); _target.Carbohydrates = decimal.Round(NutritionMath.Carbohydrates(_target.Calories, _target.Protein, _target.Fat), 1); return Task.CompletedTask; });
    private Task StartingTargets() => Run(() =>
    {
        var p = UserProfile!;
        var maintenance = NutritionMath.Maintenance(p.Weight, p.Height, p.Age, p.Coefficient, p.Activity);
        SetCalculatedTarget(NutritionMath.StartingTarget(maintenance, p.Weight, _target.Goal,
            _target.Training, _target.ProteinPreference, _target.Diet == DietPreference.Custom ? DietPreference.Balanced : _target.Diet));
        Notice = "Starting estimate filled in. Save to schedule it for tomorrow.";
        return Task.CompletedTask;
    });
    private Task ApplyPreferences() => Run(() =>
    {
        SetCalculatedTarget(NutritionMath.AllocateMacros(_target.Calories, UserProfile!.Weight, _target.Goal,
            _target.Training, _target.ProteinPreference, _target.Diet));
        Notice = "Diet preferences applied to your current calorie target. Save to schedule them for tomorrow.";
        return Task.CompletedTask;
    });
    private void SetCalculatedTarget(TargetInput target)
    {
        target.Protein = decimal.Round(target.Protein, 1);
        target.Fat = decimal.Round(target.Fat, 1);
        target.Carbohydrates = decimal.Round(target.Carbohydrates, 1);
        _target = target;
    }
    private void MarkCustom() => _target.Diet = DietPreference.Custom;
    private async Task SetTheme(ChangeEventArgs e)
    {
        _theme = e.Value?.ToString() ?? "system";
        await JS.InvokeVoidAsync("mycalTheme.set", _theme);
    }
}
