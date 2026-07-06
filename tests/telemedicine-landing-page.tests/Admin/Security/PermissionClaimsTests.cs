using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using TelemedicineLandingPage.Services.Admin.Sql;
using TelemedicineLandingPage.Services.Auth;
using TelemedicineLandingPage.Tests.Admin.Sql;

namespace TelemedicineLandingPage.Tests.Admin.Security;

public sealed class PermissionClaimsTests
{
    [Fact]
    public async Task ClaimsTransformation_AddsEffectivePermissionClaims()
    {
        using var db = TestDbHelper.CreateSeededContext();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var transformer = new DynamicPermissionClaimsTransformation(
            db,
            new EffectivePermissionResolver(db),
            cache);
        var identity = new ClaimsIdentity(
            [
                new Claim(PermissionClaimTypes.MedUserId, MedDataStoreSeed.AdminUserId.ToString()),
                new Claim(ClaimTypes.Name, "admin")
            ],
            authenticationType: "test");
        var principal = new ClaimsPrincipal(identity);

        var transformed = await transformer.TransformAsync(principal);

        Assert.Contains(transformed.Claims, claim =>
            claim.Type == PermissionClaimTypes.Permission &&
            claim.Value == "PERM_MANAGE_PERM");
        Assert.Contains(transformed.Claims, claim =>
            claim.Type == PermissionClaimTypes.PermissionsLoaded &&
            claim.Value == "true");
    }

    [Fact]
    public void ClaimsPermissionService_ChecksPermissionClaims()
    {
        var service = new ClaimsPermissionService();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(PermissionClaimTypes.Permission, "manage_users")],
            authenticationType: "test"));

        Assert.True(service.HasPermission(principal, "manage_users"));
        Assert.False(service.HasPermission(principal, "view_reports"));
    }
}
