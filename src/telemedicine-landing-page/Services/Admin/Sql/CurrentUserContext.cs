using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
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
    private readonly IMedDataChangeBus? _changeBus;
    private Guid? _cachedPermissionUserId;
    private IReadOnlyList<EffectivePermissionResolver.ResolvedPermission>? _cachedPermissions;
    private long _cachedPermissionRevision = -1;

    public event Action? StateChanged;

    public AppUser? CurrentUser { get; private set; }

    public CurrentUserContext(MedDbContext db, EffectivePermissionResolver resolver, IMedDataChangeBus? changeBus = null)
    {
        _db = db;
        _resolver = resolver;
        _changeBus = changeBus;
    }

    public void SetCurrentUser(Guid userId)
    {
        _db.ChangeTracker.Clear();
        var user = _db.Users.AsNoTracking()
            .FirstOrDefault(u => u.UserId == userId && u.Status == "active" && u.OnboardingStatus == "active")
            ?? throw new InvalidOperationException("Người dùng không tồn tại hoặc đã bị vô hiệu hóa.");
        CurrentUser = user;
        ClearPermissionCache();
        StateChanged?.Invoke();
    }

    /// <summary>Đăng nhập bằng username + password. Trả về null nếu thất bại.</summary>
    public AppUser? LoginByUsername(string username, string password)
        => LoginByUsernameDetailed(username, password).User;

    /// <summary>Đăng nhập bằng username + password và phân biệt tài khoản chưa kích hoạt.</summary>
    public LoginAttemptResult LoginByUsernameDetailed(string username, string password)
    {
        // Always read the latest activation state — admin may have approved on another tab/circuit.
        _db.ChangeTracker.Clear();

        var user = _db.Users.AsNoTracking().FirstOrDefault(u => u.Username == username && u.Status == "active");
        var inactiveUser = user is null
            ? _db.Users.AsNoTracking().FirstOrDefault(u => u.Username == username && u.Status != "active" && u.DeletedAt == null)
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
        ClearPermissionCache();
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
        ClearPermissionCache();
        StateChanged?.Invoke();
    }

    public void RefreshFromDatabase()
    {
        if (CurrentUser is null)
        {
            return;
        }

        _db.ChangeTracker.Clear();
        var userId = CurrentUser.UserId;
        var refreshed = _db.Users.AsNoTracking()
            .FirstOrDefault(user => user.UserId == userId && user.DeletedAt == null);
        if (refreshed is null ||
            !string.Equals(refreshed.Status, "active", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(refreshed.OnboardingStatus, "active", StringComparison.OrdinalIgnoreCase))
        {
            SignOut();
            return;
        }

        CurrentUser = refreshed;
        ClearPermissionCache();
        StateChanged?.Invoke();
    }

    public bool HasPermission(string permissionCode)
    {
        if (CurrentUser is null) return false;
        return GetEffectivePermissions().Any(permission =>
            string.Equals(permission.PermissionCode, permissionCode, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(permission.EffectCode, "allow", StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<EffectivePermissionResolver.ResolvedPermission> GetEffectivePermissions()
    {
        if (CurrentUser is null) return Array.Empty<EffectivePermissionResolver.ResolvedPermission>();

        var revision = _changeBus?.Revision ?? 0;
        if (_cachedPermissions is not null &&
            _cachedPermissionUserId == CurrentUser.UserId &&
            _cachedPermissionRevision == revision)
        {
            return _cachedPermissions;
        }

        var permissions = _resolver.Resolve(CurrentUser.UserId).ToArray();
        _cachedPermissionUserId = CurrentUser.UserId;
        _cachedPermissions = permissions;
        _cachedPermissionRevision = revision;
        return permissions;
    }

    private void ClearPermissionCache()
    {
        _cachedPermissionUserId = null;
        _cachedPermissions = null;
        _cachedPermissionRevision = -1;
    }

    /// <summary>Mã hóa mật khẩu bằng SHA256.</summary>
    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
