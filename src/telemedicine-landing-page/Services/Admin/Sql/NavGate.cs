using TelemedicineLandingPage.Models.Admin;

namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>
/// Filters navigation by the current user's effective permissions.
/// Each route accepts the new SQL-file permission code plus legacy codes that may
/// still exist in an already-created database.
/// </summary>
public sealed class NavGate
{
    private readonly ICurrentUserContext _userContext;

    private static readonly Dictionary<string, string[]> RoutePermissionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["/admin/to-chuc/khoa-phong"] = new[] { "SCR_ORG_DEPARTMENTS:VIEW", "PERM_PERMISSIONS_view" },
        ["/admin/to-chuc/nguoi-dung"] = new[] { "SCR_ORG_USERS:VIEW", "PERM_PERMISSIONS_view" },
        ["/admin/to-chuc/vai-tro"] = new[] { "SCR_ORG_ROLES:VIEW", "PERM_PERMISSIONS_view" },
        ["/admin/to-chuc/nhom"] = new[] { "SCR_ORG_GROUPS:VIEW", "PERM_PERMISSIONS_view" },
        ["/admin/to-chuc"] = new[] { "SCR_ORG_USERS:VIEW", "PERM_PERMISSIONS_view" },
        ["/admin/quy-trinh/tao"] = new[] { "SCR_PROCEDURE_CREATE:CREATE", "PERM_PROCEDURES_create" },
        ["/admin/quy-trinh/phe-duyet"] = new[] { "SCR_PROCEDURE_APPROVAL:APPROVE", "PERM_PROCEDURES_approve" },
        ["/admin/quy-trinh"] = new[] { "SCR_PROCEDURES:VIEW", "PERM_PROCEDURES_view" },
        ["/admin/phan-quyen"] = new[] { "SCR_PERMISSIONS:VIEW", "PERM_PERMISSIONS_view" },
        ["/admin/bao-cao/tieu-thu"] = new[] { "SCR_REPORT_CONSUMPTION:VIEW", "REPORTS:VIEW" },
        ["/admin/bao-cao"] = new[] { "SCR_REPORTS:VIEW", "REPORTS:VIEW" },
        ["/admin/danh-muc"] = new[] { "SCR_CATALOG:VIEW", "PERM_CATALOG_view" },
        ["/admin/phac-do"] = new[] { "SCR_PROTOCOLS:VIEW", "PERM_PROTOCOLS_view" },
        ["/admin/lam-sang"] = new[] { "SCR_CLINICAL_ADMIN:VIEW", "PERM_CLINICAL_view" },
        ["/admin/nhat-ky"] = new[] { "SCR_AUDIT:VIEW", "PERM_PERMISSIONS_view" },
        ["/admin/he-thong/man-hinh"] = new[] { "SCR_SYSTEM_SCREENS:VIEW", "PERM_PERMISSIONS_view" },
        ["/admin/cai-dat"] = new[] { "SCR_SETTINGS:VIEW" },
        ["/admin/ho-so"] = new[] { "SCR_PROFILE:VIEW" },
        ["/phe-duyet"] = new[] { "SCR_PERMISSION_APPROVAL:APPROVE", "PERM_PERMISSIONS_approve" },
        ["/quy-trinh-pro"] = new[] { "SCR_PROCEDURES_WORKSPACE:VIEW", "PROCEDURE_MANAGEMENT:VIEW" },
        ["/tai-nguyen"] = new[] { "SCR_RESOURCES:VIEW", "PERM_RESOURCES_view" },
        ["/dieu-phoi"] = new[] { "SCR_ORDERS:VIEW", "PERM_ORDERS_view" },
        ["/phac-do-pro"] = new[] { "SCR_PROTOCOLS_WORKSPACE:VIEW", "CLINICAL_PROTOCOLS:VIEW" },
        ["/lam-sang"] = new[] { "SCR_CLINICAL:VIEW", "PERM_CLINICAL_view" },
        ["/thong-bao"] = new[] { "SCR_NOTIFICATIONS:VIEW" },
    };

    public NavGate(ICurrentUserContext userContext)
    {
        _userContext = userContext;
    }

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

    public bool CanAccess(string route)
    {
        if (_userContext.CurrentUser is null) return false;
        if (IsSystemAdmin()) return true;

        string[]? matchedPermissions = null;
        var matchLength = 0;

        foreach (var (prefix, permissionCodes) in RoutePermissionMap)
        {
            if (route.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && prefix.Length > matchLength)
            {
                matchedPermissions = permissionCodes;
                matchLength = prefix.Length;
            }
        }

        if (matchedPermissions is null) return true;
        return matchedPermissions.Any(_userContext.HasPermission);
    }

    private bool IsSystemAdmin()
    {
        var perms = _userContext.GetEffectivePermissions();
        if (perms.Any(p => p.PermissionCode is "SCR_PERMISSIONS:DELETE" or "PERM_PERMISSIONS_delete" &&
                           p.EffectCode == "allow"))
            return true;

        return _userContext.CurrentUser?.Username == "admin";
    }
}
