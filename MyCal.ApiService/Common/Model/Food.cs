using MyCal.ApiService.Abstractions;

namespace MyCal.ApiService.Common.Model;

public class Food : DomainEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public double Calories { get; set; }
    public double? Protein { get; set; }
    public double? Carbohydrates { get; set; }
    public double? Fats { get; set; }
}