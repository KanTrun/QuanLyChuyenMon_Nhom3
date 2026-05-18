using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>
/// Triển khai ngữ cảnh người dùng hiện tại (scoped per-circuit).
/// Mặc định đăng nhập với người dùng SYSTEM_ADMIN đầu tiên.
/// </summary>
public sealed class CurrentUserContext : ICurrentUserContext
{
    private readonly IMedDataStore _store;
    private readonly EffectivePermissionResolver _resolver;

    public event Action? StateChanged;

    public AppUser? CurrentUser { get; private set; }

    public CurrentUserContext(IMedDataStore store, EffectivePermissionResolver resolver)
    {
        _store = store;
        _resolver = resolver;

        // Mặc định đăng nhập với SYSTEM_ADMIN user đầu tiên
        var sysAdminRole = _store.Roles.FirstOrDefault(r => r.Code == "SYSTEM_ADMIN");
        if (sysAdminRole is not null)
        {
            var adminUserRole = _store.UserRoles.FirstOrDefault(ur => ur.RoleId == sysAdminRole.RoleId);
            if (adminUserRole is not null)
            {
                CurrentUser = _store.Users.FirstOrDefault(u => u.UserId == adminUserRole.UserId);
            }
        }
    }

    public void SetCurrentUser(Guid userId)
    {
        var user = _store.Users.FirstOrDefault(u => u.UserId == userId && u.Status == "active")
            ?? throw new InvalidOperationException("Người dùng không tồn tại hoặc đã bị vô hiệu hóa.");
        CurrentUser = user;
        StateChanged?.Invoke();
    }

    public void SignOut()
    {
        CurrentUser = null;
        StateChanged?.Invoke();
    }

    public bool HasPermission(string permissionCode)
    {
        if (CurrentUser is null) return false;
        return _resolver.HasPermission(CurrentUser.UserId, permissionCode);
    }

    public IReadOnlyList<EffectivePermissionResolver.ResolvedPermission> GetEffectivePermissions()
    {
        if (CurrentUser is null) return Array.Empty<EffectivePermissionResolver.ResolvedPermission>();
        return _resolver.Resolve(CurrentUser.UserId);
    }
}
