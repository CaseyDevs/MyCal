using MyCal.Domain.Abstractions;
using MyCal.Domain.Enum;

namespace MyCal.Domain.Entity;

public enum Meal
{
    Breakfast, Lunch, Dinner, Snacks
}
public enum NutritionUnit
{
    Grams, Millilitres, Serving
}
public enum DiaryStatus
{
    Incomplete, Complete, ConfirmedZero
}
public enum TrainingType { GeneralActivity, ResistanceTraining }
public enum ProteinPreference { Standard, Higher }
public enum DietPreference { Custom, Balanced, LowerFat, LowerCarb }

public sealed class TargetRevision : BaseEntity
{
    public int UserId
    {
        get; set;
    }
    public DateOnly EffectiveFrom
    {
        get; set;
    }
    public decimal Calories
    {
        get; set;
    }
    public decimal Protein
    {
        get; set;
    }
    public decimal Carbohydrates
    {
        get; set;
    }
    public decimal Fat
    {
        get; set;
    }
    public decimal MaintenanceEstimate
    {
        get; set;
    }
    public GoalType Goal
    {
        get; set;
    }
    public TrainingType Training { get; set; }
    public ProteinPreference ProteinPreference { get; set; }
    public DietPreference Diet { get; set; }
}

public sealed class WeightEntry : BaseEntity
{
    public int UserId
    {
        get; set;
    }
    public DateOnly Date
    {
        get; set;
    }
    public decimal Kilograms
    {
        get; set;
    }
}

public sealed class SavedFood : BaseEntity
{
    public int UserId
    {
        get; set;
    }
    public string Fingerprint { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Brand
    {
        get; set;
    }
    public string Source { get; set; } = "Custom";
    public string? SourceId
    {
        get; set;
    }
    public string? Barcode
    {
        get; set;
    }
    public NutritionUnit Unit
    {
        get; set;
    }
    public decimal ReferenceQuantity { get; set; } = 100;
    public decimal Calories
    {
        get; set;
    }
    public decimal? Protein
    {
        get; set;
    }
    public decimal? Carbohydrates
    {
        get; set;
    }
    public decimal? Fat
    {
        get; set;
    }
    public bool IsFavourite
    {
        get; set;
    }
    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;
}

public sealed class CoachingReview : BaseEntity
{
    public int UserId
    {
        get; set;
    }
    public DateOnly Date
    {
        get; set;
    }
    public int TargetRevisionId
    {
        get; set;
    }
    public string Fingerprint { get; set; } = "";
    public string Reason { get; set; } = "";
    public decimal CurrentCalories
    {
        get; set;
    }
    public decimal? SuggestedCalories
    {
        get; set;
    }
    public decimal? WeeklyLossPercent
    {
        get; set;
    }
    public DateTime? AcceptedAt
    {
        get; set;
    }
}
