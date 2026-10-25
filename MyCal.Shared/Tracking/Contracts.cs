using System.ComponentModel.DataAnnotations;
using MyCal.Domain.Entity;
using MyCal.Domain.Enum;

namespace MyCal.Shared.Tracking;

public sealed record FoodItem(string Name, string? Brand, string Source, string? SourceId,
    string? Barcode, NutritionUnit Unit, decimal ReferenceQuantity, decimal Calories,
    decimal? Protein, decimal? Carbohydrates, decimal? Fat, int? SavedId = null, bool IsFavourite = false,
    bool RequiresBasisConfirmation = false);
public sealed record NutritionTotals(decimal Calories, decimal Protein, decimal Carbohydrates,
    decimal Fat, bool ProteinIncomplete, bool CarbohydratesIncomplete, bool FatIncomplete);
public sealed record DiaryEntry(int Id, Meal Meal, decimal Amount, FoodItem Food, NutritionTotals Totals);
public sealed record Target(int Id, DateOnly EffectiveFrom, decimal Calories, decimal Protein,
    decimal Carbohydrates, decimal Fat, decimal MaintenanceEstimate, GoalType Goal);
public sealed record DiaryDay(DateOnly Date, DiaryStatus Status, IReadOnlyList<DiaryEntry> Entries,
    NutritionTotals Totals, Target? Target);
public sealed record DaySummary(DateOnly Date, DiaryStatus Status, decimal Calories, Target? Target);
public sealed record WeightPoint(DateOnly Date, decimal Kilograms);
public sealed record ReviewResult(int Id, string Reason, decimal CurrentCalories,
    decimal? SuggestedCalories, decimal? WeeklyLossPercent, bool Accepted);
public sealed record ProgressData(IReadOnlyList<WeightPoint> Weights, int CompleteDays,
    decimal? AverageCalories, decimal? AverageProtein, decimal? AverageCarbohydrates, decimal? AverageFat);
public sealed record Profile(int Id, string Name, string Email, decimal Height, decimal Weight,
    decimal GoalWeight, int Age, Gender Coefficient, ActivityLevel Activity, string TimeZoneId);

public sealed class ProfileInput
{
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [Range(18, 100)] public int Age { get; set; } = 30;
    [Range(100, 250)] public decimal Height { get; set; } = 175;
    [Range(30, 350)] public decimal Weight { get; set; } = 75;
    [Range(30, 350)] public decimal GoalWeight { get; set; } = 70;
    public Gender Coefficient { get; set; } = Gender.Male;
    public ActivityLevel Activity { get; set; } = ActivityLevel.Sedentary;
    [Required] public string TimeZoneId { get; set; } = "Europe/London";
}

public sealed class TargetInput
{
    [Range(1200, 10000)] public decimal Calories { get; set; } = 2000;
    [Range(0, 600)] public decimal Protein { get; set; } = 120;
    [Range(0, 1500)] public decimal Carbohydrates { get; set; } = 245;
    [Range(0, 500)] public decimal Fat { get; set; } = 60;
    public GoalType Goal { get; set; } = GoalType.LoseWeight;
}

public sealed class FoodInput
{
    [Required, StringLength(200)] public string Name { get; set; } = "";
    [StringLength(200)]
    public string? Brand { get; set; }
    public NutritionUnit Unit { get; set; } = NutritionUnit.Grams;
    [Range(0.01, 10000)] public decimal ReferenceQuantity { get; set; } = 100;
    [Range(0, 100000)]
    public decimal Calories { get; set; }
    [Range(0, 10000)]
    public decimal? Protein { get; set; }
    [Range(0, 10000)]
    public decimal? Carbohydrates { get; set; }
    [Range(0, 10000)]
    public decimal? Fat { get; set; }
    public FoodItem ToFood() => new(Name.Trim(), Brand?.Trim(), "Custom", null, null,
        Unit, ReferenceQuantity, Calories, Protein, Carbohydrates, Fat);
}

public interface IFoodSearch
{
    Task<FoodSearchPage> SearchAsync(string query, CancellationToken ct = default);
    Task<FoodItem?> BarcodeAsync(string barcode, CancellationToken ct = default);
}
public sealed record FoodSearchPage(IReadOnlyList<FoodItem> Foods, string? Notice = null);
