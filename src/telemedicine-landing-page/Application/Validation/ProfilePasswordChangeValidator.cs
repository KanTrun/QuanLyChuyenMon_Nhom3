using FluentValidation;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Application.Validation;

public sealed class ProfilePasswordChangeValidator : AbstractValidator<ProfilePasswordChangeCommand>
{
    public ProfilePasswordChangeValidator(IPasswordStrengthService passwordStrength)
    {
        RuleFor(command => command.OldPassword)
            .Must((command, oldPassword) => !command.HasExistingPassword ||
                CurrentUserContext.HashPassword(oldPassword) == command.CurrentPasswordHash)
            .WithMessage("Mật khẩu hiện tại không đúng.");
        RuleFor(command => command.ConfirmPassword)
            .Equal(command => command.NewPassword).WithMessage("Mật khẩu xác nhận không khớp.");
        RuleFor(command => command.NewPassword)
            .Must((command, password) => passwordStrength.Evaluate(
                password,
                command.Username,
                command.CurrentPasswordHash).IsValid)
            .WithMessage(command => passwordStrength.Evaluate(
                command.NewPassword,
                command.Username,
                command.CurrentPasswordHash).Errors.First());
    }
}
