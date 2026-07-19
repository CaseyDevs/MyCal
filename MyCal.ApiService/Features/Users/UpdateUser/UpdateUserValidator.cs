namespace MyCal.ApiService.Features.Users.UpdateUser;

using FluentValidation;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(user => user.Name).NotEmpty().MaximumLength(100);
        RuleFor(user => user.Age).InclusiveBetween(13, 120);
        RuleFor(user => user.HeightInCm).InclusiveBetween(50, 300);
        RuleFor(user => user.WeightInKg).InclusiveBetween(20, 500);
        RuleFor(user => user.WeightGoal).InclusiveBetween(20, 500);
        RuleFor(user => user.Gender).IsInEnum();
        RuleFor(user => user.ActivityLevel).IsInEnum();
    }
}