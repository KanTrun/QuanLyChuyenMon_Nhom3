using System.Security.Cryptography;
using System.Text;
using TelemedicineLandingPage.Data;
using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>
/// Triển khai ngữ cảnh người dùng hiện tại (scoped per-circuit).
/// Đăng nhập bằng tên đăng nhập hoặc email + password (SHA256 hash).
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
        var user = _db.Users.FirstOrDefault(u => u.UserId == userId && u.Status == "active" && u.OnboardingStatus == "active")
            ?? throw new InvalidOperationException("Người dùng không tồn tại hoặc đã bị vô hiệu hóa.");
        CurrentUser = user;
        StateChanged?.Invoke();
    }

    /// <summary>Đăng nhập bằng username + password. Trả về null nếu thất bại.</summary>
    public AppUser? LoginByUsername(string username, string password)
        => LoginByUsernameDetailed(username, password).User;

    /// <summary>
    /// Đăng nhập bằng username hoặc email + mật khẩu. Trả về null nếu thất bại.
    /// Implements ICurrentUserContext.Login(string identifier, string password)
    /// </summary>
    public AppUser? Login(string identifier, string password)
    {
        var candidate = FindActiveUserByIdentifier(identifier);
        if (candidate is null) return null;

        // If account has no password set, reject here (caller expects null on failure)
        if (string.IsNullOrEmpty(candidate.PasswordHash)) return null;

        var inputHash = HashPassword(password);
        if (candidate.PasswordHash != inputHash) return null;

        if (!string.Equals(candidate.OnboardingStatus, "active", StringComparison.OrdinalIgnoreCase)) return null;

        CurrentUser = candidate;
        StateChanged?.Invoke();
        return candidate;
    }

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

        if (string.Equals(candidate.OnboardingStatus, "rejected", StringComparison.OrdinalIgnoreCase))
            return new LoginAttemptResult(LoginAttemptStatus.Rejected);

        if (!string.Equals(candidate.OnboardingStatus, "active", StringComparison.OrdinalIgnoreCase) || inactiveUser is not null)
            return new LoginAttemptResult(LoginAttemptStatus.Inactive);

        CurrentUser = candidate;
        StateChanged?.Invoke();
        return new LoginAttemptResult(LoginAttemptStatus.Success, candidate);
    }

    /// <summary>Đăng nhập chỉ bằng tên đăng nhập hoặc email (dùng cho lần đầu khi chưa đặt mật khẩu).</summary>
    public AppUser? LoginWithoutPassword(string identifier)
    {
        return null;
    }

    /// <summary>
    /// Đăng nhập chỉ bằng username (tài khoản chưa đặt mật khẩu).
    /// Implements ICurrentUserContext.LoginByUsernameOnly(string username)
    /// </summary>
    public AppUser? LoginByUsernameOnly(string username)
    {
        var user = _db.Users.FirstOrDefault(u => u.Username == username && u.DeletedAt == null);
        if (user is null) return null;

        // Only allow when the account has no password set
        if (!string.IsNullOrEmpty(user.PasswordHash)) return null;

        CurrentUser = user;
        StateChanged?.Invoke();
        return user;
    }

    private AppUser? FindActiveUserByIdentifier(string identifier)
    {
        var normalizedIdentifier = identifier.Trim().ToLowerInvariant();

        return _db.Users.FirstOrDefault(u =>
            u.Status == "active" &&
            (u.Username.ToLower() == normalizedIdentifier ||
             (u.Email != null && u.Email.ToLower() == normalizedIdentifier)));
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
