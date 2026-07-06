using Microsoft.AspNetCore.Components;
using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;
using TelemedicineLandingPage.Services.Auth;

namespace TelemedicineLandingPage.Tests.Admin.Security;

public sealed class CurrentUserAuthenticationStateProviderTests
{
    [Theory]
    [InlineData("https://localhost/admin/quy-trinh")]
    [InlineData("https://localhost/qlcm/quy-trinh")]
    [InlineData("https://localhost/quy-trinh-pro")]
    [InlineData("https://localhost/lam-sang")]
    public async Task GetAuthenticationStateAsync_ReturnsPendingPrincipalForRestoreRoutes(string uri)
    {
        var provider = new CurrentUserAuthenticationStateProvider(
            new AnonymousUserContext(),
            new TestNavigationManager(uri));

        var state = await provider.GetAuthenticationStateAsync();

        Assert.True(state.User.Identity?.IsAuthenticated);
        Assert.Equal("QLCM-Pending", state.User.Identity?.AuthenticationType);
        Assert.Contains(state.User.Claims, claim =>
            claim.Type == "qlcm_auth_state" && claim.Value == "pending_session_restore");
        Assert.DoesNotContain(state.User.Claims, claim =>
            claim.Type == PermissionClaimTypes.Permission);
    }

    [Theory]
    [InlineData("https://localhost/login")]
    [InlineData("https://localhost/administrator")]
    [InlineData("https://localhost/qlcm-public")]
    public async Task GetAuthenticationStateAsync_ReturnsAnonymousPrincipalForPublicRoutes(string uri)
    {
        var provider = new CurrentUserAuthenticationStateProvider(
            new AnonymousUserContext(),
            new TestNavigationManager(uri));

        var state = await provider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity?.IsAuthenticated ?? false);
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager(string initialUri)
            => Initialize("https://localhost/", initialUri);

        protected override void NavigateToCore(string uri, bool forceLoad)
            => Uri = ToAbsoluteUri(uri).ToString();

        protected override void NavigateToCore(string uri, NavigationOptions options)
            => Uri = ToAbsoluteUri(uri).ToString();
    }

    private sealed class AnonymousUserContext : ICurrentUserContext
    {
        public event Action? StateChanged;
        public AppUser? CurrentUser => null;
        public void SetCurrentUser(Guid userId) => throw new NotSupportedException();
        public AppUser? LoginByUsername(string username, string password) => null;
        public LoginAttemptResult LoginByUsernameDetailed(string username, string password)
            => new(LoginAttemptStatus.InvalidCredentials);
        public AppUser? LoginByUsernameOnly(string username) => null;
        public void SignOut() => StateChanged?.Invoke();
        public void RefreshFromDatabase() { }
        public bool HasPermission(string permissionCode) => false;
        public IReadOnlyList<EffectivePermissionResolver.ResolvedPermission> GetEffectivePermissions()
            => Array.Empty<EffectivePermissionResolver.ResolvedPermission>();
    }
}
