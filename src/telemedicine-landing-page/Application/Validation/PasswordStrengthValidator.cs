using Microsoft.AspNetCore.Identity;
using TelemedicineLandingPage.Models.Auth;

namespace TelemedicineLandingPage.Application.Validation;

public sealed class PasswordStrengthValidator : IPasswordValidator<ApplicationUser>
{
    private readonly IPasswordStrengthService _passwordStrength;

    public PasswordStrengthValidator(IPasswordStrengthService passwordStrength)
    {
        _passwordStrength = passwordStrength;
    }

    public Task<IdentityResult> ValidateAsync(
        UserManager<ApplicationUser> manager,
        ApplicationUser user,
        string? password)
    {
        var result = _passwordStrength.Evaluate(
            password ?? string.Empty,
            user.UserName ?? user.Email,
            user.PasswordHash);

        if (result.IsValid)
        {
            return Task.FromResult(IdentityResult.Success);
        }

        var errors = result.Errors.Select(error => new IdentityError
        {
            Code = "PasswordStrength",
            Description = error
        });
        return Task.FromResult(IdentityResult.Failed(errors.ToArray()));
    }
}
