using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MyCal.Application.Calculators;
using MyCal.Domain.Entity;
using MyCal.Domain.Enum;
using MyCal.Shared.Tracking;
using MyCal.Web.Components.Tracking;
using System.Globalization;

namespace MyCal.Web.Components.Pages;

public partial class Progress
{
    private ProgressData? _data; private ReviewResult? _review; private IReadOnlyList<ReviewResult> _reviews = [];
    private DateOnly _today, _weightDate; private decimal _kg = 75;
    protected override Task OnInitializedAsync() => Run(async () => { if (!await RequireProfile()) return; _today = await Tracker.TodayAsync(IdentityId); _weightDate = _today; _kg = UserProfile!.Weight; await Load(); });
    private async Task Load()
    {
        _data = await Tracker.ProgressAsync(IdentityId, _today);
        _reviews = await Tracker.ReviewHistoryAsync(IdentityId);
    }
    private Task SaveWeight() => Run(async () => { await Tracker.SaveWeightAsync(IdentityId, _weightDate, _kg); await Load(); _review = null; Notice = "Weigh-in saved. An existing entry on that date is updated."; });
    private void EditWeight(WeightPoint p)
    {
        _weightDate = p.Date;
        _kg = p.Kilograms;
        Notice = "Update the weight form above, then save.";
    }
    private Task DeleteWeight(DateOnly date) => Run(async () => { await Tracker.DeleteWeightAsync(IdentityId, date); _review = null; await Load(); });
    private Task Review() => Run(async () => { _review = await Tracker.ReviewAsync(IdentityId); await Load(); });
    private Task AcceptReview() => Run(async () => { await Tracker.AcceptReviewAsync(IdentityId, _review!.Id); _review = _review with { Accepted = true }; await Load(); Notice = "Suggestion accepted. Your new plan starts tomorrow."; });
    private static string Average(decimal? value) => value?.ToString("0.#") ?? "—";
    private string X(DateOnly date) => (10m + 680m * (date.DayNumber - _data!.Weights.First().Date.DayNumber) / Math.Max(1, _data.Weights.Last().Date.DayNumber - _data.Weights.First().Date.DayNumber)).ToString("0.#", CultureInfo.InvariantCulture);
    private string Y(decimal kg)
    {
        var min = _data!.Weights.Min(x => x.Kilograms) - .5m;
        var max = _data.Weights.Max(x => x.Kilograms) + .5m;
        return (180m - (kg - min) / (max - min) * 160).ToString("0.#", CultureInfo.InvariantCulture);
    }
    private string Points(IEnumerable<WeightPoint> points) => string.Join(" ", points.Select(x => $"{X(x.Date)},{Y(x.Kilograms)}"));
    private IEnumerable<List<WeightPoint>> TrendSegments()
    {
        var segment = new List<WeightPoint>();
        foreach (var point in _data!.Weights)
        {
            var window = _data.Weights.Where(x => x.Date <= point.Date && x.Date >= point.Date.AddDays(-6)).ToList();
            if (window.Count < 3 || segment.Count > 0 && point.Date.DayNumber - segment[^1].Date.DayNumber > 7)
            {
                if (segment.Count > 0)
                    yield return segment;
                segment = [];
            }
            if (window.Count >= 3)
                segment.Add(new(point.Date, window.Average(x => x.Kilograms)));
        }
        if (segment.Count > 0)
            yield return segment;
    }
}
