using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using TelemedicineLandingPage.Models.Auth;

namespace TelemedicineLandingPage.Services.Auth;

public sealed class NullPasswordGuardSignInManager : SignInManager<ApplicationUser>
{
    private readonly ILogger<SignInManager<ApplicationUser>> _logger;

    public NullPasswordGuardSignInManager(
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
        IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<ApplicationUser>> logger,
        IAuthenticationSchemeProvider schemes,
        IUserConfirmation<ApplicationUser> confirmation)
        : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
    {
        _logger = logger;
    }

    public override Task<SignInResult> CheckPasswordSignInAsync(
        ApplicationUser user,
        string password,
        bool lockoutOnFailure)
    {
        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            _logger.LogWarning("Blocked password sign-in for identity user {UserId} without password hash.", user.Id);
            return Task.FromResult(SignInResult.Failed);
        }

        return base.CheckPasswordSignInAsync(user, password, lockoutOnFailure);
    }
}
