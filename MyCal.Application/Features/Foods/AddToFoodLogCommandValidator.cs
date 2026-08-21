using FluentValidation;

namespace MyCal.Application.Features.Foods;

public sealed class AddToFoodLogCommandValidator
    : AbstractValidator<AddToFoodLogCommand>
{
    public AddToFoodLogCommandValidator()
    {
        RuleFor(command => command.FoodLogId).GreaterThan(0);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Brand).MaximumLength(200);
        RuleFor(command => command.QuantityInGrams).GreaterThan(0);
        RuleFor(command => command.CaloriesPer100G).GreaterThanOrEqualTo(0);
        RuleFor(command => command.ProteinPer100G).GreaterThanOrEqualTo(0);
        RuleFor(command => command.CarbohydratesPer100G).GreaterThanOrEqualTo(0);
        RuleFor(command => command.FatsPer100G).GreaterThanOrEqualTo(0);
    }
}
