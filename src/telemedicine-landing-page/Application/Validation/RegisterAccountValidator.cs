using FluentValidation;

namespace TelemedicineLandingPage.Application.Validation;

public sealed class RegisterAccountValidator : AbstractValidator<RegisterAccountCommand>
{
    public RegisterAccountValidator(IPasswordStrengthService passwordStrength)
    {
        RuleFor(command => command.FirstName)
            .NotEmpty().WithMessage("Vui lòng nhập họ.");
        RuleFor(command => command.LastName)
            .NotEmpty().WithMessage("Vui lòng nhập tên.");
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage("Vui lòng nhập email.")
            .EmailAddress().WithMessage("Email chua dung dinh dang.");
        RuleFor(command => command.ConfirmPassword)
            .Equal(command => command.Password).WithMessage("Mật khẩu xác nhận không khớp.");
        RuleFor(command => command.Password)
            .Must((command, password) => passwordStrength.Evaluate(password, command.Email).IsValid)
            .WithMessage(command => passwordStrength.Evaluate(command.Password, command.Email).Errors.First());
    }
}
