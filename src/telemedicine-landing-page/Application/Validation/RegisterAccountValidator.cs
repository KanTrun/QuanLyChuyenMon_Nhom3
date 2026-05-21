using FluentValidation;

namespace TelemedicineLandingPage.Application.Validation;

public sealed class RegisterAccountValidator : AbstractValidator<RegisterAccountCommand>
{
    public RegisterAccountValidator(IPasswordStrengthService passwordStrength)
    {
        RuleFor(command => command.FirstName)
            .NotEmpty().WithMessage("Vui long nhap ho.");
        RuleFor(command => command.LastName)
            .NotEmpty().WithMessage("Vui long nhap ten.");
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage("Vui long nhap email.")
            .EmailAddress().WithMessage("Email chua dung dinh dang.");
        RuleFor(command => command.ConfirmPassword)
            .Equal(command => command.Password).WithMessage("Mat khau xac nhan khong khop.");
        RuleFor(command => command.Password)
            .Must((command, password) => passwordStrength.Evaluate(password, command.Email).IsValid)
            .WithMessage(command => passwordStrength.Evaluate(command.Password, command.Email).Errors.First());
    }
}
