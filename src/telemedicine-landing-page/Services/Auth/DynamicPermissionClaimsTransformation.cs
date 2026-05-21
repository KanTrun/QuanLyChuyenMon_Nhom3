using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using TelemedicineLandingPage.Data;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Services.Auth;

public sealed class DynamicPermissionClaimsTransformation : IClaimsTransformation
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly MedDbContext _db;
    private readonly EffectivePermissionResolver _resolver;
    private readonly IMemoryCache _cache;

    public DynamicPermissionClaimsTransformation(
        MedDbContext db,
        EffectivePermissionResolver resolver,
        IMemoryCache cache)
    {
        _db = db;
        _resolver = resolver;
        _cache = cache;
    }

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true ||
            principal.HasClaim(c => c.Type == PermissionClaimTypes.PermissionsLoaded))
        {
            return Task.FromResult(principal);
        }

        if (!TryResolveMedUserId(principal, out var userId))
        {
            return Task.FromResult(principal);
        }

        var resolvedUserId = userId;
        var permissions = _cache.GetOrCreate(
            $"qlcm-permissions:{resolvedUserId:N}",
            entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                return _resolver.Resolve(resolvedUserId)
                    .Where(p => string.Equals(p.EffectCode, "allow", StringComparison.OrdinalIgnoreCase))
                    .Select(p => p.PermissionCode)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }) ?? [];

        if (principal.Identity is ClaimsIdentity identity)
        {
            identity.AddClaim(new Claim(PermissionClaimTypes.MedUserId, resolvedUserId.ToString()));
            identity.AddClaim(new Claim(PermissionClaimTypes.PermissionsLoaded, "true"));
            foreach (var permission in permissions)
            {
                identity.AddClaim(new Claim(PermissionClaimTypes.Permission, permission));
            }
        }

        return Task.FromResult(principal);
    }

    private bool TryResolveMedUserId(ClaimsPrincipal principal, out Guid userId)
    {
        var rawUserId = principal.FindFirstValue(PermissionClaimTypes.MedUserId)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(rawUserId, out var parsedUserId) && _db.Users.Any(u => u.UserId == parsedUserId))
        {
            userId = parsedUserId;
            return true;
        }

        var username = principal.Identity?.Name ?? principal.FindFirstValue(ClaimTypes.Name);
        var user = string.IsNullOrWhiteSpace(username)
            ? null
            : _db.Users.FirstOrDefault(u => u.Username == username && u.Status == "active" && u.DeletedAt == null);
        if (user is null)
        {
            userId = Guid.Empty;
            return false;
        }

        userId = user.UserId;
        return true;
    }
}
