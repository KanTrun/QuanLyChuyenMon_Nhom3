using TelemedicineLandingPage.Models.Admin;
using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>
/// Maps screens to navigation routes and VIEW permissions for the simplified page-access UI.
/// </summary>
public static class PageAccessCatalog
{
    public static readonly IReadOnlySet<string> SelfServiceScreenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "SCR_PROFILE",
    };

    /// <summary>Action codes granted together when a page is enabled in the simplified access UI.</summary>
    public static readonly IReadOnlyDictionary<string, string[]> ScreenActionBundles = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["SCR_PROCEDURES"] = ["view", "create", "update"],
        ["SCR_PROCEDURE_CREATE"] = ["view", "create"],
        ["SCR_PROCEDURE_APPROVAL"] = ["view", "approve", "publish"],
        ["SCR_PROCEDURES_WORKSPACE"] = ["view", "approve"],
        ["SCR_PROTOCOLS"] = ["view", "create", "update"],
        ["SCR_PROTOCOLS_WORKSPACE"] = ["view", "execute"],
        ["SCR_CLINICAL"] = ["view", "create", "update", "execute"],
        ["SCR_CLINICAL_ADMIN"] = ["view", "create", "update", "execute"],
        ["SCR_ORDERS"] = ["view", "create", "update"],
        ["SCR_CATALOG"] = ["view", "create", "update"],
        ["SCR_RESOURCES"] = ["view", "create", "update"],
    };

    public static readonly IReadOnlyDictionary<string, string[]> Presets = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["Nền tảng"] = ["SCR_DASHBOARD", "SCR_NOTIFICATIONS", "SCR_PROFILE"],
        ["Quy trình"] = ["SCR_PROCEDURES", "SCR_PROCEDURE_CREATE", "SCR_PROCEDURE_APPROVAL", "SCR_PROCEDURES_WORKSPACE"],
        ["Lâm sàng"] = ["SCR_CLINICAL", "SCR_CLINICAL_ADMIN", "SCR_PROTOCOLS", "SCR_PROTOCOLS_WORKSPACE"],
        ["Vận hành"] = ["SCR_CATALOG", "SCR_RESOURCES", "SCR_ORDERS", "SCR_REPORTS", "SCR_REPORT_CONSUMPTION"],
        ["Quản trị"] = ["SCR_ORG_USERS", "SCR_ORG_ROLES", "SCR_ORG_DEPARTMENTS", "SCR_ORG_GROUPS", "SCR_PERMISSIONS", "SCR_PERMISSION_APPROVAL"],
    };

    public static bool IsSelfServiceRoute(string? route)
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            return false;
        }

        var normalized = NormalizeRoute(route);
        return string.Equals(normalized, "/admin/ho-so", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "/qlcm/ho-so", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsSelfServiceScreen(string? screenCode)
        => !string.IsNullOrWhiteSpace(screenCode) && SelfServiceScreenCodes.Contains(screenCode);

    public static IReadOnlyList<PageAccessRow> BuildRows(
        IReadOnlyList<ScreenCatalog> screens,
        IReadOnlyList<MedPermission> permissions)
    {
        return screens
            .Where(screen => screen.Status == "active" && !string.IsNullOrWhiteSpace(screen.Route))
            .OrderBy(screen => AdminBusinessDisplay.Module(screen.ModuleCode))
            .ThenBy(screen => screen.Name)
            .Select(screen =>
            {
                var viewPermission = permissions
                    .Where(permission => permission.ScreenId == screen.ScreenId && permission.Status == "active")
                    .OrderBy(permission => permission.ActionCode == "view" ? 0 : 1)
                    .ThenBy(permission => permission.PermissionCode, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault(permission =>
                        string.Equals(permission.ActionCode, "view", StringComparison.OrdinalIgnoreCase)
                        || permission.PermissionCode.EndsWith(":VIEW", StringComparison.OrdinalIgnoreCase));

                return new PageAccessRow(
                    screen.ScreenId,
                    screen.ScreenCode,
                    screen.Name,
                    screen.Route!,
                    screen.ModuleCode,
                    AdminBusinessDisplay.Module(screen.ModuleCode),
                    viewPermission,
                    IsSelfServiceScreen(screen.ScreenCode));
            })
            .ToList();
    }

    public static bool UserHasRouteAccess(
        IReadOnlyList<EffectivePermissionResolver.ResolvedPermission> permissions,
        IReadOnlyList<string> routePermissionCodes,
        string route)
    {
        if (IsSelfServiceRoute(route))
        {
            return true;
        }

        if (routePermissionCodes.Count == 0)
        {
            return !NormalizeRoute(route).StartsWith("/admin", StringComparison.OrdinalIgnoreCase);
        }

        var allowed = permissions
            .Where(permission => string.Equals(permission.EffectCode, "allow", StringComparison.OrdinalIgnoreCase))
            .Select(permission => permission.PermissionCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return routePermissionCodes.Any(allowed.Contains);
    }

    public static IReadOnlyList<MedPermission> GetBundlePermissions(
        PageAccessRow page,
        IReadOnlyList<MedPermission> permissions)
    {
        if (page.IsSelfService)
        {
            return Array.Empty<MedPermission>();
        }

        var actionCodes = ScreenActionBundles.TryGetValue(page.ScreenCode, out var bundle)
            ? bundle
            : ["view"];

        return permissions
            .Where(permission =>
                permission.ScreenId == page.ScreenId
                && permission.Status == "active"
                && actionCodes.Contains(permission.ActionCode, StringComparer.OrdinalIgnoreCase))
            .OrderBy(permission => permission.PermissionCode, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string NormalizeRoute(string route)
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            return "/";
        }

        var path = route.Split('?', '#')[0];
        var normalized = path.StartsWith('/') ? path : "/" + path;
        return normalized.Length > 1 ? normalized.TrimEnd('/') : normalized;
    }

    public sealed record PageAccessRow(
        Guid ScreenId,
        string ScreenCode,
        string ScreenName,
        string Route,
        string? ModuleCode,
        string ModuleName,
        MedPermission? ViewPermission,
        bool IsSelfService);
}
