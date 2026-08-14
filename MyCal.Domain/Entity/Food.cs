using MyCal.Domain.Abstractions;

namespace MyCal.Domain.Entity;

public class Food : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public double Calories { get; set; }
    public double? Protein { get; set; }
    public double? Carbohydrates { get; set; }
    public double? Fats { get; set; }
}