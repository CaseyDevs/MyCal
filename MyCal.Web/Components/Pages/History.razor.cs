using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MyCal.Application.Calculators;
using MyCal.Domain.Entity;
using MyCal.Domain.Enum;
using MyCal.Shared.Tracking;
using MyCal.Web.Components.Tracking;
using System.Globalization;

namespace MyCal.Web.Components.Pages;

public partial class History
{
    private DateOnly _today, _month = new(2026, 1, 1); private IReadOnlyList<DaySummary> _days = [];
    protected override Task OnInitializedAsync() => Run(async () => { if (!await RequireProfile()) return; _today = await Tracker.TodayAsync(IdentityId); _month = new(_today.Year, _today.Month, 1); await Load(); });
    private async Task Load()
    {
        var last = _month.AddMonths(1).AddDays(-1);
        if (last > _today)
            last = _today;
        _days = await Tracker.HistoryAsync(IdentityId, _month, last);
    }
    private Task Move(int amount) => Run(async () => { var next = _month.AddMonths(amount); if (next > _today || next.Year < 2000) return; _month = next; await Load(); });
    private Task ChangeMonth(ChangeEventArgs e) => Run(async () => { if (DateOnly.TryParse(e.Value + "-01", out var month) && month <= _today && month.Year >= 2000) { _month = month; await Load(); } });
}
