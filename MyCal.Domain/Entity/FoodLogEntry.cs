using MyCal.Domain.Abstractions;

namespace MyCal.Domain.Entity;

public class FoodLogEntry : BaseEntity
{
    public int FoodLogId
    {
        get; set;
    }
    public FoodLog FoodLog { get; set; } = null!;

    public Food? Food
    {
        get; set;
    }
    public int? FoodId
    {
        get; set;
    }

    public int QuantityInGrams
    {
        get; set;
    }
    // A diary owns its nutrition snapshot. Catalogue changes never rewrite history.
    public string? Name
    {
        get; set;
    }
    public string? Brand
    {
        get; set;
    }
    public string Source { get; set; } = "Legacy";
    public string? SourceId
    {
        get; set;
    }
    public string? Barcode
    {
        get; set;
    }
    public Meal Meal { get; set; } = Meal.Snacks;
    public NutritionUnit Unit
    {
        get; set;
    }
    public decimal Amount
    {
        get; set;
    }
    public decimal ReferenceQuantity { get; set; } = 100;
    public decimal CaloriesPerReference
    {
        get; set;
    }
    public decimal? ProteinPerReference
    {
        get; set;
    }
    public decimal? CarbohydratesPerReference
    {
        get; set;
    }
    public decimal? FatPerReference
    {
        get; set;
    }
}
