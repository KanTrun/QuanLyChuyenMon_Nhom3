using Microsoft.AspNetCore.Authorization;

namespace TelemedicineLandingPage.Services.Auth;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(params string[] permissionCodes)
    {
        PermissionCodes = permissionCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .ToArray();
    }

    public IReadOnlyList<string> PermissionCodes { get; }
}
