using MyCal.Application.Calculators;
using MyCal.Domain.Entity;
using MyCal.Domain.Enum;
using MyCal.Shared.Tracking;

namespace MyCal.Tests;

[TestClass]
public sealed class CoachingTests
{
    private readonly DateOnly _today = new(2026, 10, 25);
    private Target Target(decimal calories = 2000, decimal maintenance = 2500) => new(1, _today.AddDays(-28), calories, 120, 230, 65, maintenance, GoalType.LoseWeight);
    private List<DiaryDay> Days(decimal calories = 2000) => Enumerable.Range(1, 28).Select(i => new DiaryDay(_today.AddDays(-i), DiaryStatus.Complete, [], new(calories, 120, 230, 65, false, false, false), null)).ToList();
    private List<WeightPoint> Weights(decimal loss) => Enumerable.Range(0, 28).Select(i => new WeightPoint(_today.AddDays(-28 + i), i < 14 ? 100m : 100m - loss * 2)).ToList();
    [TestMethod]
    [DataRow(0.10, 1900)]
    [DataRow(0.80, 2100)]
    public void SuggestsOnlyOneHundredCalories(double loss, int expected)
        => Assert.AreEqual((decimal)expected, WeeklyCoach.Evaluate(_today, Target(), Days(), Weights((decimal)loss), false).SuggestedCalories);
    [TestMethod]
    [DataRow(0.25)]
    [DataRow(0.50)]
    [DataRow(0.75)]
    public void HoldsInsideInclusiveLossRange(double loss)
        => Assert.IsNull(WeeklyCoach.Evaluate(_today, Target(), Days(), Weights((decimal)loss), false).SuggestedCalories);
    [TestMethod]
    public void MissingOrPartialFoodDaysHold()
    {
        var days = Days();
        days.RemoveAt(0);
        Assert.IsNull(WeeklyCoach.Evaluate(_today, Target(), days, Weights(.1m), false).SuggestedCalories);
        days = Days();
        days[0] = days[0] with
        {
            Status = DiaryStatus.Incomplete
        };
        StringAssert.Contains(WeeklyCoach.Evaluate(_today, Target(), days, Weights(.1m), false).Reason, "Complete all 28");
    }
    [TestMethod]
    public void SparseWeightsAndIntakeOutsideTenPercentHold()
    {
        var weights = Weights(.1m).Where(x => x.Date < _today.AddDays(-7)).ToList();
        StringAssert.Contains(WeeklyCoach.Evaluate(_today, Target(), Days(), weights, false).Reason, "three");
        StringAssert.Contains(WeeklyCoach.Evaluate(_today, Target(), Days(2200.01m), Weights(.1m), false).Reason, "10%");
        Assert.IsNotNull(WeeklyCoach.Evaluate(_today, Target(), Days(2200), Weights(.1m), false).SuggestedCalories);
    }
    [TestMethod]
    public void NewOrScheduledTargetHoldsAndManualGoalDoesNotGetFatLossAdvice()
    {
        Assert.IsNull(WeeklyCoach.Evaluate(_today, Target() with
        {
            EffectiveFrom = _today.AddDays(-27)
        }, Days(), Weights(.1m), false).SuggestedCalories);
        Assert.IsNull(WeeklyCoach.Evaluate(_today, Target(), Days(), Weights(.1m), true).SuggestedCalories);
        Assert.IsNull(WeeklyCoach.Evaluate(_today, Target() with
        {
            Goal = GoalType.MaintainWeight
        }, Days(), Weights(.1m), false).SuggestedCalories);
    }
    [TestMethod]
    public void CalorieAndDeficitFloorsAreInclusive()
    {
        Assert.AreEqual(1500m, WeeklyCoach.Evaluate(_today, Target(1600, 2000), Days(1600), Weights(.1m), false).SuggestedCalories);
        Assert.IsNull(WeeklyCoach.Evaluate(_today, Target(1599, 2000), Days(1599), Weights(.1m), false).SuggestedCalories);
        Assert.IsNull(WeeklyCoach.Evaluate(_today, Target(2000, 2600), Days(), Weights(.1m), false).SuggestedCalories);
    }
    [TestMethod]
    public void IsolatedWaterSpikeDoesNotForceAdjustment()
    {
        var weights = Weights(.5m);
        weights[3] = weights[3] with
        {
            Kilograms = 103
        };
        Assert.IsNull(WeeklyCoach.Evaluate(_today, Target(), Days(), weights, false).SuggestedCalories);
    }
}
