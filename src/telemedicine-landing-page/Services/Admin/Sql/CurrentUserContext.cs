using System.Security.Cryptography;
using System.Text;
using TelemedicineLandingPage.Data;
using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>
/// Triển khai ngữ cảnh người dùng hiện tại (scoped per-circuit).
/// Đăng nhập bằng username + password (SHA256 hash).
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
    }

    public void SetCurrentUser(Guid userId)
    {
        var user = _db.Users.FirstOrDefault(u => u.UserId == userId && u.Status == "active")
            ?? throw new InvalidOperationException("Người dùng không tồn tại hoặc đã bị vô hiệu hóa.");
        CurrentUser = user;
        StateChanged?.Invoke();
    }

    /// <summary>Đăng nhập bằng username + password. Trả về null nếu thất bại.</summary>
    public AppUser? LoginByUsername(string username, string password)
    {
        var user = _db.Users.FirstOrDefault(u => u.Username == username && u.Status == "active");
        if (user is null) return null;

        // Nếu chưa đặt mật khẩu (NULL hoặc rỗng) → cho đăng nhập luôn
        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            CurrentUser = user;
            StateChanged?.Invoke();
            return user;
        }

        // Kiểm tra mật khẩu (SHA256)
        var inputHash = HashPassword(password);
        if (user.PasswordHash != inputHash)
            return null;

        CurrentUser = user;
        StateChanged?.Invoke();
        return user;
    }

    /// <summary>Đăng nhập chỉ bằng username (dùng cho lần đầu khi chưa đặt mật khẩu).</summary>
    public AppUser? LoginByUsernameOnly(string username)
    {
        var user = _db.Users.FirstOrDefault(u => u.Username == username && u.Status == "active");
        if (user is null) return null;

        // Chỉ cho phép nếu chưa có password_hash
        if (!string.IsNullOrEmpty(user.PasswordHash)) return null;

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

    /// <summary>Mã hóa mật khẩu bằng SHA256.</summary>
    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
