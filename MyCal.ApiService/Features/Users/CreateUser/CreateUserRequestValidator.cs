using FluentValidation;

namespace MyCal.ApiService.Features.Users.CreateUser;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(user => user.Name).NotEmpty().MaximumLength(100);
        RuleFor(user => user.Email).NotEmpty().EmailAddress();
        RuleFor(user => user.Age).InclusiveBetween(13, 120);
        RuleFor(user => user.HeightInCm).InclusiveBetween(50, 300);
        RuleFor(user => user.WeightInKg).InclusiveBetween(20, 500);
        RuleFor(user => user.WeightGoal).InclusiveBetween(20, 500);
        RuleFor(user => user.Gender).IsInEnum();
        RuleFor(user => user.ActivityLevel).IsInEnum();
    }
}
