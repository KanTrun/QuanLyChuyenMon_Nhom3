using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Services.Auth;

public sealed class CurrentUserAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly ICurrentUserContext _currentUser;
    private readonly NavigationManager _navigation;

    public CurrentUserAuthenticationStateProvider(ICurrentUserContext currentUser, NavigationManager navigation)
    {
        _currentUser = currentUser;
        _navigation = navigation;
        _currentUser.StateChanged += OnUserChanged;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => Task.FromResult(new AuthenticationState(BuildPrincipal()));

    private ClaimsPrincipal BuildPrincipal()
    {
        var user = _currentUser.CurrentUser;
        if (user is null)
        {
            var path = new Uri(_navigation.Uri).AbsolutePath;
            if (IsSessionRestorePath(path))
            {
                return new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim("qlcm_auth_state", "pending_session_restore") },
                    "QLCM-Pending"));
            }

            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username)
        };
        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        }

        claims.AddRange(_currentUser.GetEffectivePermissions()
            .Where(permission => string.Equals(permission.EffectCode, "allow", StringComparison.OrdinalIgnoreCase))
            .Select(permission => new Claim(PermissionClaimTypes.Permission, permission.PermissionCode)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "QLCM"));
    }

    public static bool IsSessionRestorePath(string path)
    {
        if (StartsWithRouteSegment(path, "/admin") ||
            StartsWithRouteSegment(path, "/qlcm"))
        {
            return true;
        }

        return path.Equals("/phe-duyet", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/quy-trinh-pro", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/tai-nguyen", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/dieu-phoi", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/phac-do-pro", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/lam-sang", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/thong-bao", StringComparison.OrdinalIgnoreCase);
    }

    private static bool StartsWithRouteSegment(string path, string prefix)
        => path.Equals(prefix, StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase);

    private void OnUserChanged()
        => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    public void Dispose()
    {
        _currentUser.StateChanged -= OnUserChanged;
    }
}
