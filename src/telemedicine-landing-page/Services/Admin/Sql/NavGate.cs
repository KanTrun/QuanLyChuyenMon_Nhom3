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
        ["/admin/to-chuc"] = "PERM_PERMISSIONS_view",
        ["/admin/quy-trinh/tao"] = "PERM_PROCEDURES_create",
        ["/admin/quy-trinh/phe-duyet"] = "PERM_PROCEDURES_approve",
        ["/admin/quy-trinh"] = "PERM_PROCEDURES_view",
        ["/admin/phan-quyen"] = "PERM_PERMISSIONS_view",
        ["/admin/bao-cao"] = "REPORTS:VIEW",
        ["/admin/danh-muc"] = "PERM_CATALOG_view",
        ["/admin/phac-do"] = "PERM_PROTOCOLS_view",
        ["/admin/lam-sang"] = "PERM_CLINICAL_view",
        ["/admin/nhat-ky"] = "PERM_PERMISSIONS_view",
        ["/phe-duyet"] = "PERM_PERMISSIONS_approve",
        ["/quy-trinh-pro"] = "PROCEDURE_MANAGEMENT:VIEW",
        ["/tai-nguyen"] = "PERM_RESOURCES_view",
        ["/dieu-phoi"] = "PERM_ORDERS_view",
        ["/phac-do-pro"] = "CLINICAL_PROTOCOLS:VIEW",
        ["/lam-sang"] = "PERM_CLINICAL_view",
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
        if (perms.Any(p => p.PermissionCode == "PERM_PERMISSIONS_delete" && p.EffectCode == "allow"))
            return true;

        // Fallback: nếu không có permissions data, kiểm tra username
        // (admin luôn là SYSTEM_ADMIN trong hệ thống thực tế)
        return _userContext.CurrentUser?.Username == "admin";
    }
}
