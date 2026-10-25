using System.ComponentModel.DataAnnotations;
using MyCal.Domain.Entity;
using MyCal.Domain.Enum;
using MyCal.Shared.Tracking;

namespace MyCal.Application.Calculators;

public sealed record CoachingDecision(string Reason, decimal? SuggestedCalories = null, decimal? WeeklyLossPercent = null);

public static class WeeklyCoach
{
    // Product policy: intentionally cautious, transparent, and independently testable.
    public static CoachingDecision Evaluate(DateOnly today, Target target, IReadOnlyList<DiaryDay> days,
        IReadOnlyList<WeightPoint> weights, bool hasScheduledTarget)
    {
        var start = today.AddDays(-28);
        if (target.Goal != GoalType.LoseWeight)
            return new("Weekly adjustments currently support fat-loss targets. Your manual targets remain available.");
        if (hasScheduledTarget)
            return new("A new target is already scheduled. Review again after 28 complete days on that target.");
        if (target.EffectiveFrom > start)
            return new("Keep your calorie target unchanged for 28 complete days before an adjustment.");
        var window = days.Where(x => x.Date >= start && x.Date < today).ToList();
        if (window.Select(x => x.Date).Distinct().Count() != 28 || window.Any(x => x.Status == DiaryStatus.Incomplete))
            return new("Complete all 28 previous days of food logging. Missing or partial days are never treated as zero intake.");
        for (var week = 0; week < 4; week++)
        {
            var from = start.AddDays(week * 7);
            if (weights.Where(x => x.Date >= from && x.Date < from.AddDays(7)).Select(x => x.Date).Distinct().Count() < 3)
                return new("Log your weight on at least three different days in each of the previous four weeks.");
        }
        var average = window.Average(x => x.Totals.Calories);
        if (Math.Abs(average - target.Calories) > target.Calories * .1m)
            return new("Your average logged intake differs from your target by more than 10%. Build consistency before changing the target.");
        var oldMean = weights.Where(x => x.Date >= start && x.Date < start.AddDays(14)).Average(x => x.Kilograms);
        var newMean = weights.Where(x => x.Date >= start.AddDays(14) && x.Date < today).Average(x => x.Kilograms);
        var loss = (oldMean - newMean) / 2 / oldMean * 100;
        if (loss is >= .25m and <= .75m)
            return new("Your weight trend is within the beta's 0.25–0.75% weekly loss range. Keep your current target.", WeeklyLossPercent: loss);
        var proposed = target.Calories + (loss < .25m ? -100 : 100);
        if (proposed < target.Calories && (proposed < 1500 || proposed < target.MaintenanceEstimate * .75m))
            return new("Hold your target: a further cut would cross the beta's 1,500 kcal or 25% estimated-deficit limit.", WeeklyLossPercent: loss);
        try
        { NutritionMath.AdjustCalories(target, proposed); }
        catch (ValidationException)
        {
            return new("Hold your target: this change cannot preserve your protein and macro allocation within the beta's limits. Set manual targets instead.", WeeklyLossPercent: loss);
        }
        return new(loss < .25m
            ? "Your recorded weight trend is slower than the selected range. Consider 100 kcal less per day."
            : "Your recorded weight trend is faster than the selected range. Consider 100 kcal more per day.", proposed, loss);
    }
}
