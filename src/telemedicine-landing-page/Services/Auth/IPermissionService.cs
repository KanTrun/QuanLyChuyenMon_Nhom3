using System.Security.Claims;

namespace TelemedicineLandingPage.Services.Auth;

public interface IPermissionService
{
    bool HasPermission(ClaimsPrincipal user, params string[] permissionCodes);
    IReadOnlyList<string> GetPermissionCodes(ClaimsPrincipal user);
}
