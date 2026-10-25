using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using MyCal.Application.Calculators;
using MyCal.Domain.Entity;
using MyCal.Domain.Enum;
using MyCal.Infrastructure.Nutrition;
using MyCal.Shared.Tracking;

namespace MyCal.Tests;

[TestClass]
public sealed class NutritionTests
{
    [TestMethod]
    [DataRow(Gender.Male, 2076)]
    [DataRow(Gender.Female, 1877)]
    public void MifflinUsesIndependentReferenceFixtures(Gender coefficient, int expected)
        => Assert.AreEqual((decimal)expected, NutritionMath.Maintenance(75, 180, 30, coefficient, ActivityLevel.Sedentary));
    [TestMethod]
    public void StartingMacrosUseBodyWeightAndRemainingEnergy()
    {
        var target = NutritionMath.StartingTarget(2076, 75, GoalType.LoseWeight);
        Assert.AreEqual(1765m, target.Calories);
        Assert.AreEqual(120m, target.Protein);
        Assert.IsTrue(Math.Abs(58.8333333333m - target.Fat) < .000001m);
        Assert.IsTrue(Math.Abs(188.875m - target.Carbohydrates) < .000001m);
        NutritionMath.ValidateTarget(target);
    }
    [TestMethod]
    public void FractionalPortionsPreserveDeclaredCaloriesAndUnknownMacros()
    {
        var food = new FoodItem("Example", null, "Custom", null, null, NutritionUnit.Grams, 100, 250, 10, null, 5);
        var portion = NutritionMath.Portion(food, 75.5m);
        Assert.AreEqual(188.75m, portion.Calories);
        Assert.AreEqual(7.55m, portion.Protein);
        Assert.IsTrue(portion.CarbohydratesIncomplete);
        Assert.AreEqual(3.775m, portion.Fat);
        // The declared energy need not equal 4/4/9 of the reported macros.
        Assert.AreNotEqual(portion.Calories, portion.Protein * 4 + portion.Fat * 9);
    }
    [TestMethod]
    public void MillilitresAndServingsScaleAgainstTheirOwnReference()
    {
        var drink = new FoodItem("Drink", null, "Custom", null, null, NutritionUnit.Millilitres, 100, 50, 3, 5, 2);
        Assert.AreEqual(125m, NutritionMath.Portion(drink, 250).Calories);
        var serving = drink with
        {
            Unit = NutritionUnit.Serving,
            ReferenceQuantity = 1,
            Calories = 180
        };
        Assert.AreEqual(90m, NutritionMath.Portion(serving, .5m).Calories);
    }
    [TestMethod]
    public void ImpossibleMacrosAndUnconfirmedUnitsAreRejected()
    {
        Assert.ThrowsExactly<ValidationException>(() => NutritionMath.Carbohydrates(1500, 400, 50));
        var food = new FoodItem("Food", null, "OFF", null, null, NutritionUnit.Grams, 100, 100, null, null, null, RequiresBasisConfirmation: true);
        Assert.ThrowsExactly<ValidationException>(() => NutritionMath.Portion(food, 100));
        Assert.ThrowsExactly<ValidationException>(() => NutritionMath.ValidateTarget(new() { Calories = 2000, Protein = 100, Fat = 100, Carbohydrates = 100 }));
    }
    [TestMethod]
    public void MissingFoodAndTinyReferencesAreRejectedAtTheBoundary()
    {
        Assert.ThrowsExactly<ValidationException>(() => NutritionMath.ValidateFood(null));
        var food = new FoodItem("Food", null, "Custom", null, null, NutritionUnit.Grams, .000001m, 100, 0, 0, 0);
        Assert.ThrowsExactly<ValidationException>(() => NutritionMath.Portion(food, 100));
        Assert.ThrowsExactly<ValidationException>(() => NutritionMath.ValidateFood(food with { ReferenceQuantity = 100, Source = null! }));
    }
    [TestMethod]
    [DataRow("2026-07-01T23:30:00Z", "Europe/London", "2026-07-02")]
    [DataRow("2026-10-25T00:30:00Z", "Europe/London", "2026-10-25")]
    [DataRow("2026-10-25T01:30:00Z", "Europe/London", "2026-10-25")]
    [DataRow("2026-03-29T00:30:00Z", "Europe/London", "2026-03-29")]
    [DataRow("2026-03-29T01:30:00Z", "Europe/London", "2026-03-29")]
    [DataRow("2026-10-25T01:30:00Z", "America/Los_Angeles", "2026-10-24")]
    public void LocalDatesRespectMidnightAndDaylightSaving(string utc, string zone, string expected)
        => Assert.AreEqual(DateOnly.Parse(expected), NutritionMath.LocalToday(new FixedClock(DateTimeOffset.Parse(utc)), zone));
    [TestMethod]
    public void UsdaUsesSpecificEnergyAndRemovesFibreOnlyWhenKnown()
    {
        using var document = JsonDocument.Parse("""{"fdcId":1,"description":"Test","foodNutrients":[{"nutrientId":2047,"unitName":"KCAL","value":300},{"nutrientId":2048,"unitName":"KCAL","value":280},{"nutrientId":1005,"unitName":"G","value":40},{"nutrientId":1079,"unitName":"G","value":8}]}""");
        var food = FoodCatalog.ParseUsda(document.RootElement)!;
        Assert.AreEqual(280m, food.Calories);
        Assert.AreEqual(32m, food.Carbohydrates);
        Assert.IsNull(food.Protein);
        using var missing = JsonDocument.Parse("""{"fdcId":2,"description":"Unknown fibre","foodNutrients":[{"nutrientId":1008,"unitName":"KCAL","value":300},{"nutrientId":1005,"unitName":"G","value":40}]}""");
        Assert.IsNull(FoodCatalog.ParseUsda(missing.RootElement)!.Carbohydrates);
    }
    [TestMethod]
    public void OpenFoodFactsConvertsKilojoulesAndRequiresPackageBasisCheck()
    {
        using var doc = JsonDocument.Parse("""{"code":"0123456789012","product_name":"Test","nutriments":{"energy-kj_100g":418.4,"fat_100g":0,"proteins_100g":2}}""");
        var food = FoodCatalog.ParseOff(doc.RootElement)!;
        Assert.AreEqual(100m, food.Calories);
        Assert.AreEqual(0m, food.Fat);
        Assert.IsNull(food.Carbohydrates);
        Assert.IsTrue(food.RequiresBasisConfirmation);
    }
    [TestMethod]
    [DataRow("5000159484695", true)]
    [DataRow("012345678905", true)]
    [DataRow("96385074", true)]
    [DataRow("5000159484696", false)]
    [DataRow("123", false)]
    [DataRow("abcdefghijkl", false)]
    public void BarcodeValidationKeepsLeadingZerosAndChecksTheCheckDigit(string code, bool valid)
        => Assert.AreEqual(valid, FoodCatalog.ValidBarcode(code));
}

public sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
