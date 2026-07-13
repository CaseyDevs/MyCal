using FluentValidation;
using MyCal.ApiService.Common.Dto.Profile;

namespace MyCal.ApiService.Validator;

/// <summary>
/// Fluent validation approach for profile requests
/// </summary>
public sealed class ProfileRequestValidator
    : AbstractValidator<ProfileRequestDto>
{
    public ProfileRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Age)
            .InclusiveBetween(13, 120);

        RuleFor(x => x.HeightInCm)
            .InclusiveBetween(50, 300);

        RuleFor(x => x.WeightInKg)
            .InclusiveBetween(20, 500);

        RuleFor(x => x.WeightGoal)
            .InclusiveBetween(20, 500);

        RuleFor(x => x.Gender)
            .IsInEnum();

        RuleFor(x => x.ActivityLevel)
            .IsInEnum();
    }

}
