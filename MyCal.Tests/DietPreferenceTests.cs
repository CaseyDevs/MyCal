using System.ComponentModel.DataAnnotations;
using MyCal.Application.Calculators;
using MyCal.Domain.Entity;
using MyCal.Domain.Enum;
using MyCal.Shared.Tracking;

namespace MyCal.Tests;

[TestClass]
public class DietPreferenceTests
{
    [TestMethod]
    [DataRow(GoalType.LoseWeight, TrainingType.ResistanceTraining, ProteinPreference.Higher, 165)]
    [DataRow(GoalType.MaintainWeight, TrainingType.ResistanceTraining, ProteinPreference.Higher, 150)]
    [DataRow(GoalType.GainWeight, TrainingType.ResistanceTraining, ProteinPreference.Higher, 150)]
    [DataRow(GoalType.LoseWeight, TrainingType.GeneralActivity, ProteinPreference.Higher, 150)]
    [DataRow(GoalType.LoseWeight, TrainingType.ResistanceTraining, ProteinPreference.Standard, 120)]
    [DataRow(GoalType.MaintainWeight, TrainingType.GeneralActivity, ProteinPreference.Standard, 120)]
    public void ProteinFixturesUseTotalBodyWeight(GoalType goal, TrainingType training, ProteinPreference preference, int expected)
    {
        var target = NutritionMath.AllocateMacros(2000, 75, goal, training, preference, DietPreference.Balanced);
        Assert.AreEqual((decimal)expected, target.Protein);
        Assert.AreEqual(2000m, target.Calories, "Higher protein must fit the existing energy budget.");
    }

    [TestMethod]
    [DataRow(DietPreference.LowerFat, 400, 235)]
    [DataRow(DietPreference.Balanced, 600, 185)]
    [DataRow(DietPreference.LowerCarb, 800, 135)]
    public void CuttingFixturesAllocateRemainingCalories(DietPreference diet, int fatCalories, int carbs)
    {
        var target = NutritionMath.AllocateMacros(2000, 75, GoalType.LoseWeight,
            TrainingType.ResistanceTraining, ProteinPreference.Higher, diet);
        Assert.AreEqual(165m, target.Protein);
        Assert.AreEqual((decimal)fatCalories, target.Fat * 9, .000001m);
        Assert.AreEqual((decimal)carbs, target.Carbohydrates, .000001m);
        Assert.AreEqual(2000m, target.Protein * 4 + target.Carbohydrates * 4 + target.Fat * 9, .000001m);
    }

    [TestMethod]
    public void ImpossiblePresetsInvalidEnumsAndCustomAllocationAreRejected()
    {
        Assert.ThrowsExactly<ValidationException>(() => NutritionMath.AllocateMacros(1500, 150, GoalType.LoseWeight,
            TrainingType.ResistanceTraining, ProteinPreference.Higher, DietPreference.LowerCarb));
        Assert.ThrowsExactly<ValidationException>(() => NutritionMath.ProteinFactor(GoalType.LoseWeight,
            (TrainingType)99, ProteinPreference.Higher));
        Assert.ThrowsExactly<ValidationException>(() => NutritionMath.AllocateMacros(2000, 75, GoalType.LoseWeight,
            TrainingType.ResistanceTraining, ProteinPreference.Higher, DietPreference.Custom));
        var input = new TargetInput { Training = (TrainingType)99 };
        Assert.ThrowsExactly<ValidationException>(() => NutritionMath.ValidateTarget(input));
    }

    [TestMethod]
    [DataRow(1900)]
    [DataRow(2100)]
    public void CoachingKeepsProteinAndManualNonProteinSplit(int calories)
    {
        // Use a round 50:50 non-protein split: 140 g protein + 80 g fat + 180 g carbs = 2,000 kcal.
        var target = new Target(1, new(2026, 10, 1), 2000, 140, 180, 80, 2500, GoalType.LoseWeight,
            TrainingType.ResistanceTraining, ProteinPreference.Higher, DietPreference.Custom);
        var adjusted = NutritionMath.AdjustCalories(target, calories);
        Assert.AreEqual(140m, adjusted.Protein);
        Assert.AreEqual((calories - 560m) / 2 / 9, adjusted.Fat, .000001m);
        Assert.AreEqual((calories - 560m) / 2 / 4, adjusted.Carbohydrates, .000001m);
        Assert.AreEqual(target.Diet, adjusted.Diet);
        Assert.AreEqual(target.Training, adjusted.Training);
        Assert.AreEqual(target.ProteinPreference, adjusted.ProteinPreference);
    }

    [TestMethod]
    public void CoachingWithholdsChangesThatCannotRetainProtein()
    {
        var today = new DateOnly(2026, 10, 25);
        var target = new Target(1, today.AddDays(-28), 2400, 590, 10, 0, 3000, GoalType.LoseWeight);
        var days = Enumerable.Range(1, 28).Select(i => new DiaryDay(today.AddDays(-i), DiaryStatus.Complete, [],
            new(2400, 590, 10, 0, false, false, false), target)).ToList();
        var weights = Enumerable.Range(1, 28).Select(i => new WeightPoint(today.AddDays(-i), 100)).ToList();
        var decision = WeeklyCoach.Evaluate(today, target, days, weights, false);
        Assert.IsNull(decision.SuggestedCalories);
        StringAssert.Contains(decision.Reason, "cannot preserve your protein");
    }
}
