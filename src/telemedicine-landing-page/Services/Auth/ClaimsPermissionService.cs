using System.Security.Claims;

namespace TelemedicineLandingPage.Services.Auth;

public sealed class ClaimsPermissionService : IPermissionService
{
    public bool HasPermission(ClaimsPrincipal user, params string[] permissionCodes)
    {
        if (user.Identity?.IsAuthenticated != true || permissionCodes.Length == 0)
        {
            return false;
        }

        var granted = GetPermissionCodes(user).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return permissionCodes.Any(granted.Contains);
    }

    public IReadOnlyList<string> GetPermissionCodes(ClaimsPrincipal user)
        => user.Claims
            .Where(c => c.Type == PermissionClaimTypes.Permission)
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
