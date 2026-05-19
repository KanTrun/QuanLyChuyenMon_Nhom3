using TelemedicineLandingPage.Models.Admin;
using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>
/// Lọc mục điều hướng dựa trên quyền hiệu lực của người dùng hiện tại.
/// Ánh xạ route → permission code để ẩn/hiện menu.
/// </summary>
public sealed class NavGate
{
    private readonly ICurrentUserContext _userContext;

    /// <summary>Ánh xạ tiền tố route → mã quyền cần thiết.</summary>
    private static readonly Dictionary<string, string> RoutePermissionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["/admin"] = "PERM_VIEW_DASHBOARD",
        ["/admin/to-chuc"] = "PERM_MANAGE_PERM",
        ["/admin/quy-trinh"] = "PERM_MANAGE_PROC",
        ["/admin/phan-quyen"] = "PERM_MANAGE_PERM",
        ["/admin/bao-cao"] = "PERM_VIEW_REPORT",
        ["/admin/danh-muc"] = "PERM_MANAGE_PROC",
        ["/admin/phac-do"] = "PERM_MANAGE_PROC",
        ["/admin/lam-sang"] = "PERM_CREATE_ORDER",
        ["/admin/nhat-ky"] = "PERM_MANAGE_PERM",
        ["/admin/cai-dat"] = "PERM_VIEW_DASHBOARD",
        ["/phe-duyet"] = "PERM_APPROVE_PROC",
        ["/quy-trinh-pro"] = "PERM_MANAGE_PROC",
        ["/tai-nguyen"] = "PERM_MANAGE_PROC",
        ["/dieu-phoi"] = "PERM_CREATE_ORDER",
        ["/phac-do-pro"] = "PERM_MANAGE_PROC",
        ["/lam-sang"] = "PERM_CREATE_ORDER",
        ["/thong-bao"] = "PERM_VIEW_DASHBOARD",
    };

    public NavGate(ICurrentUserContext userContext)
    {
        _userContext = userContext;
    }

    /// <summary>Lọc danh sách mục điều hướng theo quyền người dùng hiện tại.</summary>
    public IReadOnlyList<AdminNavItem> Filter(IReadOnlyList<AdminNavItem> items)
    {
        if (_userContext.CurrentUser is null)
            return Array.Empty<AdminNavItem>();

        var result = new List<AdminNavItem>();
        foreach (var item in items)
        {
            if (!CanAccess(item.Url))
                continue;

            if (item.Children is { Count: > 0 } children)
            {
                var filteredChildren = children.Where(c => CanAccess(c.Url)).ToList();
                if (filteredChildren.Count > 0)
                {
                    result.Add(item with { Children = filteredChildren });
                }
            }
            else
            {
                result.Add(item);
            }
        }
        return result;
    }

    /// <summary>Kiểm tra người dùng hiện tại có quyền truy cập route hay không.</summary>
    public bool CanAccess(string route)
    {
        if (_userContext.CurrentUser is null) return false;

        // SYSTEM_ADMIN luôn có quyền truy cập tất cả
        if (IsSystemAdmin()) return true;

        // Tìm route khớp dài nhất
        string? matchedPermission = null;
        int matchLength = 0;

        foreach (var (prefix, permCode) in RoutePermissionMap)
        {
            if (route.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && prefix.Length > matchLength)
            {
                matchedPermission = permCode;
                matchLength = prefix.Length;
            }
        }

        // Nếu không có ánh xạ, cho phép truy cập (route công khai)
        if (matchedPermission is null) return true;

        return _userContext.HasPermission(matchedPermission);
    }

    /// <summary>Kiểm tra người dùng hiện tại có phải SYSTEM_ADMIN không.</summary>
    private bool IsSystemAdmin()
    {
        // Kiểm tra qua effective permissions trước
        var perms = _userContext.GetEffectivePermissions();
        if (perms.Any(p => p.PermissionCode == "PERM_MANAGE_PERM" && p.EffectCode == "allow"))
            return true;

        // Fallback: nếu không có permissions data, kiểm tra username
        // (admin luôn là SYSTEM_ADMIN trong hệ thống thực tế)
        return _userContext.CurrentUser?.Username == "admin";
    }
}
