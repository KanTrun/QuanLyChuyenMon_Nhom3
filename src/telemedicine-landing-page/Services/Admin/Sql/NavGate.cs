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

    private static readonly Dictionary<string, string> AdminToWorkspaceRouteMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["/admin"] = "/qlcm",
        ["/admin/quy-trinh"] = "/qlcm/quy-trinh",
        ["/admin/quy-trinh/tao"] = "/qlcm/quy-trinh/tao",
        ["/admin/quy-trinh/phe-duyet"] = "/qlcm/quy-trinh/phe-duyet",
        ["/admin/danh-muc"] = "/qlcm/danh-muc",
        ["/admin/phac-do"] = "/qlcm/phac-do",
        ["/admin/bao-cao"] = "/qlcm/bao-cao",
        ["/admin/bao-cao/tieu-thu"] = "/qlcm/bao-cao/tieu-thu",
        ["/admin/lam-sang"] = "/qlcm/lam-sang",
        ["/admin/ho-so"] = "/qlcm/ho-so",
    };

    private static readonly Dictionary<string, string[]> RoutePermissionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["/admin"] = new[] { "SCR_DASHBOARD:VIEW", "PERM_VIEW_DASHBOARD" },
        ["/admin/to-chuc/khoa-phong"] = new[] { "SCR_ORG_DEPARTMENTS:VIEW", "PERM_PERMISSIONS_view" },
        ["/admin/to-chuc/nguoi-dung"] = new[] { "SCR_ORG_USERS:VIEW", "PERM_PERMISSIONS_view" },
        ["/admin/to-chuc/vai-tro"] = new[] { "SCR_ORG_ROLES:VIEW", "PERM_PERMISSIONS_view" },
        ["/admin/to-chuc/nhom"] = new[] { "SCR_ORG_GROUPS:VIEW", "PERM_PERMISSIONS_view" },
        ["/admin/to-chuc"] = new[] { "SCR_ORG_USERS:VIEW", "PERM_PERMISSIONS_view" },
        ["/admin/quy-trinh/tao"] = new[] { "SCR_PROCEDURES:CREATE", "PERM_PROCEDURES_create" },
        ["/admin/quy-trinh/phe-duyet"] = new[] { "SCR_PROCEDURES:APPROVE", "PERM_PROCEDURES_approve" },
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
            var canAccessParent = CanAccess(item.Url);

            if (item.Children is { Count: > 0 } children)
            {
                var filteredChildren = children.Where(c => CanAccess(c.Url)).Select(ToDisplayItem).ToList();
                if (canAccessParent || filteredChildren.Count > 0)
                {
                    result.Add(ToDisplayItem(item with { Children = filteredChildren }));
                }
            }
            else if (canAccessParent)
            {
                result.Add(ToDisplayItem(item));
            }
        }

        return result;
    }

    public string GetDisplayRoute(string route)
    {
        if (_userContext.CurrentUser is null || IsSystemAdmin())
        {
            return route;
        }

        return RewriteRoute(route, AdminToWorkspaceRouteMap);
    }

    public bool CanAccess(string route)
    {
        if (_userContext.CurrentUser is null) return false;
        if (IsSystemAdmin()) return true;

        var normalizedRoute = ToPermissionRoute(NormalizeRoute(route));
        var matchedPermissions = GetRoutePermissionCodes(normalizedRoute);
        if (matchedPermissions.Count == 0)
        {
            return !normalizedRoute.StartsWith("/admin", StringComparison.OrdinalIgnoreCase);
        }

        return matchedPermissions.Any(_userContext.HasPermission);
    }

    public IReadOnlyList<string> GetRoutePermissionCodes(string route)
    {
        var normalizedRoute = ToPermissionRoute(NormalizeRoute(route));
        string[]? matchedPermissions = null;
        var matchLength = 0;

        foreach (var (prefix, permissionCodes) in RoutePermissionMap)
        {
            if (IsRouteMatch(normalizedRoute, prefix) && prefix.Length > matchLength)
            {
                matchedPermissions = permissionCodes;
                matchLength = prefix.Length;
            }
        }

        return matchedPermissions ?? Array.Empty<string>();
    }

    public bool IsSystemAdmin()
    {
        var perms = _userContext.GetEffectivePermissions();
        if (perms.Any(p =>
                (string.Equals(p.PermissionCode, "SCR_PERMISSIONS:DELETE", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(p.PermissionCode, "PERM_PERMISSIONS_delete", StringComparison.OrdinalIgnoreCase)) &&
                string.Equals(p.EffectCode, "allow", StringComparison.OrdinalIgnoreCase)))
            return true;

        return string.Equals(_userContext.CurrentUser?.Username, "admin", StringComparison.OrdinalIgnoreCase);
    }

    private AdminNavItem ToDisplayItem(AdminNavItem item)
    {
        var children = item.Children?.Select(ToDisplayItem).ToList();
        return item with { Url = GetDisplayRoute(item.Url), Children = children };
    }

    private static string ToPermissionRoute(string route)
    {
        if (string.Equals(route, "/qlcm", StringComparison.OrdinalIgnoreCase))
        {
            return "/admin";
        }

        return route.StartsWith("/qlcm/", StringComparison.OrdinalIgnoreCase)
            ? "/admin" + route["/qlcm".Length..]
            : route;
    }

    private static string RewriteRoute(string route, IReadOnlyDictionary<string, string> routeMap)
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            return route;
        }

        var suffixIndex = route.IndexOfAny(new[] { '?', '#' });
        var path = suffixIndex >= 0 ? route[..suffixIndex] : route;
        var suffix = suffixIndex >= 0 ? route[suffixIndex..] : string.Empty;
        var normalizedPath = NormalizeRoute(path);

        return routeMap.TryGetValue(normalizedPath, out var rewrittenPath)
            ? rewrittenPath + suffix
            : route;
    }

    private static string NormalizeRoute(string route)
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            return "/";
        }

        var withoutQuery = route.Split('?', '#')[0];
        if (Uri.TryCreate(withoutQuery, UriKind.Absolute, out var absolute))
        {
            withoutQuery = absolute.AbsolutePath;
        }

        var normalized = withoutQuery.StartsWith('/') ? withoutQuery : "/" + withoutQuery;
        return normalized.Length > 1 ? normalized.TrimEnd('/') : normalized;
    }

    private static bool IsRouteMatch(string route, string prefix)
    {
        if (!route.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        return route.Length == prefix.Length || route[prefix.Length] == '/';
    }
}
