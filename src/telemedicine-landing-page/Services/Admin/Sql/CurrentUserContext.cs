using TelemedicineLandingPage.Data;
using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>
/// Triển khai ngữ cảnh người dùng hiện tại (scoped per-circuit).
/// Không tự động đăng nhập — người dùng phải đăng nhập qua /login.
/// </summary>
public sealed class CurrentUserContext : ICurrentUserContext
{
    private readonly MedDbContext _db;
    private readonly EffectivePermissionResolver _resolver;

    public event Action? StateChanged;

    public AppUser? CurrentUser { get; private set; }

    public CurrentUserContext(MedDbContext db, EffectivePermissionResolver resolver)
    {
        _db = db;
        _resolver = resolver;
        // Không tự động đăng nhập — người dùng phải xác thực qua /login
    }

    public void SetCurrentUser(Guid userId)
    {
        var user = _db.Users.FirstOrDefault(u => u.UserId == userId && u.Status == "active")
            ?? throw new InvalidOperationException("Người dùng không tồn tại hoặc đã bị vô hiệu hóa.");
        CurrentUser = user;
        StateChanged?.Invoke();
    }

    /// <summary>Đăng nhập bằng username (không kiểm tra mật khẩu — xem ghi chú bên dưới).</summary>
    /// <remarks>
    /// TODO: Thêm kiểm tra mật khẩu khi bảng users có cột password_hash.
    /// Hiện tại SQL script không có cột password nên chỉ kiểm tra username + status.
    /// </remarks>
    public AppUser? LoginByUsername(string username)
    {
        var user = _db.Users.FirstOrDefault(u => u.Username == username && u.Status == "active");
        if (user is null) return null;
        CurrentUser = user;
        StateChanged?.Invoke();
        return user;
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
