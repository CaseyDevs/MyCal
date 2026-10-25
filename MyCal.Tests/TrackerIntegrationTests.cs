using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using MyCal.Domain.Entity;
using MyCal.Domain.Enum;
using MyCal.Shared.Tracking;

namespace MyCal.Tests;

[TestClass, DoNotParallelize]
public sealed class TrackerIntegrationTests
{
    private static TestStore _store = null!;
    private static readonly DateOnly Today = new(2026, 10, 25);
    private static FoodItem Food => new("Oats", "Example", "Custom", null, null, NutritionUnit.Grams, 100, 380, 13, 60, 7);
    [ClassInitialize] public static async Task Setup(TestContext _) => _store = await TestStore.CreateAsync();
    [ClassCleanup] public static async Task Cleanup() => await _store.DisposeAsync();
    private static async Task<string> Person()
    {
        var id = Guid.NewGuid().ToString();
        await _store.Tracker.SaveProfileAsync(id, "test@example.com", new()
        {
            Name = "Tester",
            Height = 180,
            Weight = 75,
            GoalWeight = 70,
            Age = 30
        });
        return id;
    }
    [TestMethod]
    public async Task ManualTargetsRemainAvailableWhenStartingEstimateIsOutsideLimits()
    {
        var id = Guid.NewGuid().ToString();
        await _store.Tracker.SaveProfileAsync(id, "small@example.com", new()
        { Name = "Manual target", Height = 100, Weight = 30, GoalWeight = 30, Age = 18 });
        Assert.AreEqual(0, (await _store.Tracker.TargetsAsync(id)).Count);
        await _store.Tracker.SaveTargetAsync(id, new()
        { Calories = 1200, Protein = 48, Fat = 40, Carbohydrates = 162, Goal = GoalType.MaintainWeight });
        Assert.AreEqual(1200m, (await _store.Tracker.DiaryAsync(id, Today)).Target!.Calories);
    }
    [TestMethod]
    public async Task EmptyReadsDoNotCreateRows()
    {
        var id = await Person();
        await using var db = await _store.Factory.CreateDbContextAsync();
        var before = await db.FoodLogs.CountAsync();
        var day = await _store.Tracker.DiaryAsync(id, Today.AddDays(-7));
        Assert.AreEqual(0, day.Entries.Count);
        Assert.IsNull(day.Target);
        Assert.AreEqual(before, await db.FoodLogs.CountAsync());
    }
    [TestMethod]
    public async Task SnapshotTotalsSurviveFoodChangesEditsDeletesAndCopying()
    {
        var id = await Person();
        var entry = await _store.Tracker.AddEntryAsync(id, Today, Meal.Breakfast, 75.5m, Food);
        Assert.AreEqual(286.9m, (await _store.Tracker.DiaryAsync(id, Today)).Totals.Calories);
        await _store.Tracker.SaveFoodAsync(id, Food with
        {
            Calories = 900
        }, true);
        Assert.AreEqual(286.9m, (await _store.Tracker.DiaryAsync(id, Today)).Totals.Calories);
        await _store.Tracker.SetStatusAsync(id, Today, DiaryStatus.Complete);
        await _store.Tracker.UpdateEntryAsync(id, entry, Meal.Lunch, 50);
        var edited = await _store.Tracker.DiaryAsync(id, Today);
        Assert.AreEqual(190m, edited.Totals.Calories);
        Assert.AreEqual(DiaryStatus.Incomplete, edited.Status);
        await _store.Tracker.CopyAsync(id, Today, Today.AddDays(-1), Meal.Lunch);
        Assert.AreEqual(190m, (await _store.Tracker.DiaryAsync(id, Today.AddDays(-1))).Totals.Calories);
        await _store.Tracker.DeleteEntryAsync(id, entry);
        Assert.AreEqual(0m, (await _store.Tracker.DiaryAsync(id, Today)).Totals.Calories);
        Assert.AreEqual(190m, (await _store.Tracker.DiaryAsync(id, Today.AddDays(-1))).Totals.Calories);
    }
    [TestMethod]
    public async Task FiveAccountsStayPrivateAndTenConcurrentWritesDoNotDuplicateDays()
    {
        var people = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => Person()));
        var writes = Enumerable.Range(0, 10).Select(i => _store.Tracker.AddEntryAsync(people[i % 5], Today, Meal.Snacks, 10, Food));
        await Task.WhenAll(writes);
        foreach (var id in people)
        {
            var day = await _store.Tracker.DiaryAsync(id, Today);
            Assert.AreEqual(2, day.Entries.Count);
            Assert.AreEqual(76m, day.Totals.Calories);
        }
        var foreign = (await _store.Tracker.DiaryAsync(people[0], Today)).Entries[0].Id;
        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _store.Tracker.UpdateEntryAsync(people[1], foreign, Meal.Dinner, 500));
        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _store.Tracker.DeleteEntryAsync(people[1], foreign));
        await using var db = await _store.Factory.CreateDbContextAsync();
        var ownerId = (await _store.Tracker.ProfileAsync(people[0]))!.Id;
        var logId = await db.FoodLogs.Where(x => x.UserId == ownerId).Select(x => x.Id).SingleAsync();
        Assert.IsNull(await _store.Tracker.LegacyLogDateAsync(people[1], logId));
    }
    [TestMethod]
    public async Task TargetsStayHistoricalAndTimezoneChangesNeverMoveLogs()
    {
        var id = await Person();
        var original = (await _store.Tracker.DiaryAsync(id, Today)).Target!;
        await _store.Tracker.SaveTargetAsync(id, new()
        {
            Calories = 2000,
            Protein = 125,
            Fat = 60,
            Carbohydrates = 240
        });
        Assert.AreEqual(original.Calories, (await _store.Tracker.DiaryAsync(id, Today)).Target!.Calories);
        Assert.AreEqual(2000m, (await _store.Tracker.DiaryAsync(id, Today.AddDays(1))).Target!.Calories);
        await _store.Tracker.AddEntryAsync(id, Today.AddDays(-1), Meal.Dinner, 100, Food);
        await _store.Tracker.SaveProfileAsync(id, "test@example.com", new()
        {
            Name = "Tester",
            TimeZoneId = "Pacific/Auckland"
        });
        Assert.AreEqual(380m, (await _store.Tracker.DiaryAsync(id, Today.AddDays(-1))).Totals.Calories);
    }
    [TestMethod]
    public async Task CompletedZeroDaysMustBeExplicitAndEditsReopenDays()
    {
        var id = await Person();
        await Assert.ThrowsExactlyAsync<ValidationException>(() => _store.Tracker.SetStatusAsync(id, Today, DiaryStatus.Complete));
        await _store.Tracker.SetStatusAsync(id, Today, DiaryStatus.ConfirmedZero);
        Assert.AreEqual(DiaryStatus.ConfirmedZero, (await _store.Tracker.DiaryAsync(id, Today)).Status);
        await _store.Tracker.AddEntryAsync(id, Today, Meal.Lunch, 100, Food);
        Assert.AreEqual(DiaryStatus.Incomplete, (await _store.Tracker.DiaryAsync(id, Today)).Status);
        await Assert.ThrowsExactlyAsync<ValidationException>(() => _store.Tracker.SetStatusAsync(id, Today, DiaryStatus.ConfirmedZero));
    }
    [TestMethod]
    public async Task UnknownMacrosStayPartialAndIncompleteDaysAreExcludedFromAverages()
    {
        var id = await Person();
        await _store.Tracker.AddEntryAsync(id, Today.AddDays(-1), Meal.Lunch, 100, Food with
        {
            Protein = null
        });
        await _store.Tracker.SetStatusAsync(id, Today.AddDays(-1), DiaryStatus.Complete);
        await _store.Tracker.AddEntryAsync(id, Today.AddDays(-2), Meal.Lunch, 100, Food);
        var progress = await _store.Tracker.ProgressAsync(id, Today);
        Assert.AreEqual(1, progress.CompleteDays);
        Assert.AreEqual(380m, progress.AverageCalories);
        Assert.IsNull(progress.AverageProtein);
    }
    [TestMethod]
    public async Task ReviewIsRepeatableStaleSafePrivateAndSingleAcceptance()
    {
        var id = await Person();
        var other = await Person();
        var owner = (await _store.Tracker.ProfileAsync(id))!.Id;
        await using (var db = await _store.Factory.CreateDbContextAsync())
        {
            var t = await db.Targets.SingleAsync(x => x.UserId == owner);
            t.EffectiveFrom = Today.AddDays(-28);
            t.Calories = 2000;
            t.MaintenanceEstimate = 2600;
            t.Protein = 120;
            t.Fat = 60;
            t.Carbohydrates = 245;
            // Make a 100 kcal reduction permissible under the 25% deficit rule.
            t.MaintenanceEstimate = 2500;
            await db.SaveChangesAsync();
        }
        var meal = Food with
        {
            Calories = 2000
        };
        for (var i = 1; i <= 28; i++)
        {
            var date = Today.AddDays(-i);
            await _store.Tracker.AddEntryAsync(id, date, Meal.Dinner, 100, meal);
            await _store.Tracker.SetStatusAsync(id, date, DiaryStatus.Complete);
            await _store.Tracker.SaveWeightAsync(id, date, i > 14 ? 100 : 99.8m);
        }
        var review = await _store.Tracker.ReviewAsync(id);
        Assert.AreEqual(1900m, review.SuggestedCalories);
        Assert.AreEqual(review.Id, (await _store.Tracker.ReviewAsync(id)).Id);
        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _store.Tracker.AcceptReviewAsync(other, review.Id));
        await _store.Tracker.SaveWeightAsync(id, Today.AddDays(-1), 99.7m);
        await Assert.ThrowsExactlyAsync<ValidationException>(() => _store.Tracker.AcceptReviewAsync(id, review.Id));
        var fresh = await _store.Tracker.ReviewAsync(id);
        await _store.Tracker.AcceptReviewAsync(id, fresh.Id);
        await Assert.ThrowsExactlyAsync<ValidationException>(() => _store.Tracker.AcceptReviewAsync(id, fresh.Id));
        Assert.AreEqual(2000m, (await _store.Tracker.DiaryAsync(id, Today)).Target!.Calories);
        Assert.AreEqual(1900m, (await _store.Tracker.DiaryAsync(id, Today.AddDays(1))).Target!.Calories);
        Assert.IsNull((await _store.Tracker.ReviewAsync(id)).SuggestedCalories);
    }
}
