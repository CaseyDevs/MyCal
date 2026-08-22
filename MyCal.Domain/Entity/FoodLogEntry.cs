using MyCal.Domain.Abstractions;

namespace MyCal.Domain.Entity;

public class FoodLogEntry : BaseEntity
{
    public int FoodLogId { get; set; }
    public FoodLog FoodLog { get; set; } = null!;

    public Food Food { get; set; } = null!;
    public int FoodId { get; set; }

    public int QuantityInGrams { get; set; }
}
