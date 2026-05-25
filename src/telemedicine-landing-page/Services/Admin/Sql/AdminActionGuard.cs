using TelemedicineLandingPage.Services.Admin;

namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>
/// Centralized action-level guard for mutating admin operations.
/// Supports the current SQL `SCR_*:ACTION` permissions and older `PERM_*` aliases
/// kept by tests or previously seeded databases.
/// </summary>
public sealed class AdminActionGuard
{
    private readonly ICurrentUserContext _userContext;
    private readonly IToastService _toasts;
    private readonly ProcedureRuntimeGuard _runtimeGuard;

    private static readonly Dictionary<string, string[]> PermissionAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SCR_PROCEDURES:CREATE"] = ["PERM_MANAGE_PROC", "PERM_PROCEDURES_create"],
        ["SCR_PROCEDURES:UPDATE"] = ["PERM_MANAGE_PROC", "PERM_PROCEDURES_update", "PERM_PROCEDURES_manage"],
        ["SCR_PROCEDURES:DELETE"] = ["PERM_MANAGE_PROC", "PERM_PROCEDURES_delete"],
        ["SCR_PROCEDURES:APPROVE"] = ["PERM_APPROVE_PROC", "PERM_PROCEDURES_approve"],
        ["SCR_PROCEDURES:PUBLISH"] = ["PERM_APPROVE_PROC", "PERM_PROCEDURES_publish"],
        ["SCR_PROCEDURES_WORKSPACE:APPROVE"] = ["SCR_PROCEDURES:APPROVE", "PERM_APPROVE_PROC"],
        ["SCR_PROCEDURES_WORKSPACE:PUBLISH"] = ["SCR_PROCEDURES:PUBLISH", "SCR_PROCEDURES:APPROVE", "PERM_APPROVE_PROC"],

        ["SCR_PROTOCOLS:CREATE"] = ["PERM_PROTOCOLS_create"],
        ["SCR_PROTOCOLS:UPDATE"] = ["PERM_PROTOCOLS_update"],
        ["SCR_PROTOCOLS:DELETE"] = ["PERM_PROTOCOLS_delete"],
        ["SCR_PROTOCOLS:APPROVE"] = ["PERM_PROTOCOLS_approve"],
        ["SCR_PROTOCOLS:PUBLISH"] = ["PERM_PROTOCOLS_publish", "PERM_PROTOCOLS_approve"],

        ["SCR_PERMISSIONS:CREATE"] = ["PERM_MANAGE_PERM", "PERM_PERMISSIONS_create"],
        ["SCR_PERMISSIONS:UPDATE"] = ["PERM_MANAGE_PERM", "PERM_PERMISSIONS_update"],
        ["SCR_PERMISSIONS:DELETE"] = ["PERM_MANAGE_PERM", "PERM_PERMISSIONS_delete"],
        ["SCR_PERMISSIONS:APPROVE"] = ["PERM_MANAGE_PERM", "PERM_PERMISSIONS_approve"],
        ["SCR_PERMISSIONS:CONFIGURE"] = ["PERM_MANAGE_PERM", "PERM_PERMISSIONS_configure"],
        ["SCR_PERMISSION_APPROVAL:APPROVE"] = ["SCR_PERMISSIONS:APPROVE", "PERM_MANAGE_PERM", "PERM_PERMISSIONS_approve"],

        ["SCR_ORG_DEPARTMENTS:CREATE"] = ["PERM_MANAGE_PERM", "PERM_PERMISSIONS_create"],
        ["SCR_ORG_DEPARTMENTS:UPDATE"] = ["PERM_MANAGE_PERM", "PERM_PERMISSIONS_update"],
        ["SCR_ORG_DEPARTMENTS:DELETE"] = ["PERM_MANAGE_PERM", "PERM_PERMISSIONS_delete"],
        ["SCR_ORG_USERS:CREATE"] = ["PERM_MANAGE_PERM", "PERM_PERMISSIONS_create"],
        ["SCR_ORG_USERS:UPDATE"] = ["PERM_MANAGE_PERM", "PERM_PERMISSIONS_update"],
        ["SCR_ORG_USERS:DELETE"] = ["PERM_MANAGE_PERM", "PERM_PERMISSIONS_delete"],
        ["SCR_ORG_ROLES:CREATE"] = ["PERM_MANAGE_PERM", "PERM_PERMISSIONS_create"],
        ["SCR_ORG_ROLES:UPDATE"] = ["PERM_MANAGE_PERM", "PERM_PERMISSIONS_update"],
        ["SCR_ORG_ROLES:DELETE"] = ["PERM_MANAGE_PERM", "PERM_PERMISSIONS_delete"],
        ["SCR_ORG_GROUPS:CREATE"] = ["PERM_MANAGE_PERM", "PERM_PERMISSIONS_create"],
        ["SCR_ORG_GROUPS:UPDATE"] = ["PERM_MANAGE_PERM", "PERM_PERMISSIONS_update"],
        ["SCR_ORG_GROUPS:DELETE"] = ["PERM_MANAGE_PERM", "PERM_PERMISSIONS_delete"],

        ["SCR_CATALOG:CREATE"] = ["PERM_CATALOG_create"],
        ["SCR_CATALOG:UPDATE"] = ["PERM_CATALOG_update"],
        ["SCR_CATALOG:DELETE"] = ["PERM_CATALOG_delete"],
        ["SCR_RESOURCES:CREATE"] = ["PERM_RESOURCES_create"],
        ["SCR_RESOURCES:UPDATE"] = ["PERM_RESOURCES_update"],
        ["SCR_RESOURCES:DELETE"] = ["PERM_RESOURCES_delete"],

        ["SCR_ORDERS:CREATE"] = ["PERM_CREATE_ORDER", "PERM_ORDERS_create"],
        ["SCR_ORDERS:UPDATE"] = ["PERM_ORDERS_update"],
        ["SCR_ORDERS:DELETE"] = ["PERM_ORDERS_delete"],
        ["SCR_ORDERS:EXECUTE"] = ["PERM_ORDERS_execute", "PERM_ORDERS_update"],

        ["SCR_CLINICAL:CREATE"] = ["PERM_CLINICAL_create"],
        ["SCR_CLINICAL:UPDATE"] = ["PERM_CLINICAL_update"],
        ["SCR_CLINICAL:EXECUTE"] = ["PERM_CLINICAL_execute", "PERM_CLINICAL_update"],
        ["SCR_CLINICAL_ADMIN:CREATE"] = ["SCR_CLINICAL:CREATE", "PERM_CLINICAL_create"],
        ["SCR_CLINICAL_ADMIN:UPDATE"] = ["SCR_CLINICAL:UPDATE", "PERM_CLINICAL_update"],
        ["SCR_CLINICAL_ADMIN:EXECUTE"] = ["SCR_CLINICAL:EXECUTE", "PERM_CLINICAL_execute", "PERM_CLINICAL_update"],

        ["SCR_PROFILE:UPDATE"] = ["PERM_PROFILE_update"],
        ["SCR_SETTINGS:UPDATE"] = ["SCR_PROFILE:UPDATE"],
        ["SCR_NOTIFICATIONS:UPDATE"] = ["PERM_NOTIFICATIONS_update"],
    };

    public AdminActionGuard(
        ICurrentUserContext userContext,
        IToastService toasts,
        ProcedureRuntimeGuard runtimeGuard)
    {
        _userContext = userContext;
        _toasts = toasts;
        _runtimeGuard = runtimeGuard;
    }

    public bool CanDo(string permissionCode, string? actionName = null)
    {
        if (HasPermission(permissionCode))
        {
            var runtimeDecision = _runtimeGuard.EvaluatePermission(permissionCode);
            if (runtimeDecision.Allowed)
            {
                if (runtimeDecision.WarnOnly && runtimeDecision.Message is not null)
                {
                    _toasts.Show("Cảnh báo quy trình", runtimeDecision.Message, ToastVariant.Warning);
                }

                return true;
            }

            _toasts.Show(
                "Không đúng quy trình",
                runtimeDecision.Message ?? "Thao tác này đang bị quy trình chuyên môn chặn.",
                ToastVariant.Warning);
            return false;
        }

        _toasts.Show(
            "Không có quyền",
            actionName is null
                ? $"Tài khoản hiện tại thiếu quyền {permissionCode}."
                : $"Tài khoản hiện tại không được phép {actionName}.",
            ToastVariant.Warning);
        return false;
    }

    public bool HasPermission(params string[] permissionCodes)
    {
        if (_userContext.CurrentUser is null)
        {
            return false;
        }

        if (string.Equals(_userContext.CurrentUser.Username, "admin", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return permissionCodes
            .SelectMany(ExpandPermissionCodes)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Any(_userContext.HasPermission);
    }

    private static IEnumerable<string> ExpandPermissionCodes(string permissionCode)
    {
        yield return permissionCode;

        if (PermissionAliases.TryGetValue(permissionCode, out var aliases))
        {
            foreach (var alias in aliases)
            {
                yield return alias;
            }
        }
    }
}
