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
        => LoginByUsernameDetailed(username, password).User;

    /// <summary>Đăng nhập bằng username + password và phân biệt tài khoản chưa kích hoạt.</summary>
    public LoginAttemptResult LoginByUsernameDetailed(string username, string password)
    {
        var user = _db.Users.FirstOrDefault(u => u.Username == username && u.Status == "active");
        var inactiveUser = user is null
            ? _db.Users.FirstOrDefault(u => u.Username == username && u.Status != "active" && u.DeletedAt == null)
            : null;
        var candidate = user ?? inactiveUser;
        if (candidate is null) return new LoginAttemptResult(LoginAttemptStatus.InvalidCredentials);

        // Production guard: active accounts without a password must set one
        // through a verified reset/setup flow before they can sign in.
        if (string.IsNullOrEmpty(candidate.PasswordHash))
        {
            if (inactiveUser is not null)
                return new LoginAttemptResult(LoginAttemptStatus.Inactive);

            return new LoginAttemptResult(LoginAttemptStatus.PasswordNotSet);
        }

        // Kiểm tra mật khẩu (SHA256)
        var inputHash = HashPassword(password);
        if (candidate.PasswordHash != inputHash)
            return new LoginAttemptResult(LoginAttemptStatus.InvalidCredentials);

        if (inactiveUser is not null)
            return new LoginAttemptResult(LoginAttemptStatus.Inactive);

        CurrentUser = candidate;
        StateChanged?.Invoke();
        return new LoginAttemptResult(LoginAttemptStatus.Success, candidate);
    }

    /// <summary>Đăng nhập chỉ bằng username (dùng cho lần đầu khi chưa đặt mật khẩu).</summary>
    public AppUser? LoginByUsernameOnly(string username)
    {
        return null;
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
