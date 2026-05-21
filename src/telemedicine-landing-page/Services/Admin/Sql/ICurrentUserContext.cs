using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>
/// Ngữ cảnh người dùng hiện tại (scoped per-circuit).
/// Cung cấp xác thực và kiểm tra quyền.
/// </summary>
public interface ICurrentUserContext
{
    /// <summary>Sự kiện khi trạng thái người dùng thay đổi (đăng nhập/đăng xuất).</summary>
    event Action? StateChanged;

    /// <summary>Người dùng hiện tại (null nếu chưa đăng nhập).</summary>
    AppUser? CurrentUser { get; }

    /// <summary>Đặt người dùng hiện tại theo UserId.</summary>
    void SetCurrentUser(Guid userId);

    /// <summary>Đăng nhập bằng tên đăng nhập hoặc email + mật khẩu. Trả về null nếu thất bại.</summary>
    AppUser? Login(string identifier, string password);

    /// <summary>Đăng nhập chỉ bằng tên đăng nhập hoặc email (tài khoản chưa đặt mật khẩu).</summary>
    AppUser? LoginWithoutPassword(string identifier);

    /// <summary>Đăng xuất người dùng hiện tại.</summary>
    void SignOut();

    /// <summary>Kiểm tra người dùng hiện tại có quyền cụ thể hay không.</summary>
    bool HasPermission(string permissionCode);

    /// <summary>Lấy danh sách quyền hiệu lực của người dùng hiện tại.</summary>
    IReadOnlyList<EffectivePermissionResolver.ResolvedPermission> GetEffectivePermissions();
}
