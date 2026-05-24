using System.Text.Json;
using TelemedicineLandingPage.Models.Admin.Sql;
using SqlDepartment = TelemedicineLandingPage.Models.Admin.Sql.Department;

namespace TelemedicineLandingPage.Models.Admin;

/// <summary>
/// Shared presentation helper that translates technical admin codes into
/// hospital-facing wording. Raw ids/codes should stay in technical details.
/// </summary>
public static class AdminBusinessDisplay
{
    public const string Empty = "—";

    public static string Lookup(IReadOnlyList<LookupEntry> entries, string? code)
        => entries.FirstOrDefault(e => Same(e.Code, code))?.Name ?? Fallback(code);

    public static string RecordStatus(string? code) => Lookup(MedLookups.RecordStatuses, code);
    public static string VersionStatus(string? code) => Lookup(MedLookups.VersionStatuses, code);
    public static string Scope(string? code) => Lookup(MedLookups.DepartmentScopeTypes, NormalizeScope(code));
    public static string Effect(string? code) => Lookup(MedLookups.PermissionEffects, code);
    public static string ChangeStatus(string? code) => Lookup(MedLookups.PermissionChangeStatuses, code);
    public static string ChangeOperation(string? code) => Lookup(MedLookups.PermissionChangeOperations, code);
    public static string ProcedureType(string? code) => Lookup(MedLookups.ProcedureTypes, code);
    public static string ServiceType(string? code) => Lookup(MedLookups.ServiceTypes, code);
    public static string ResourceType(string? code) => Lookup(MedLookups.ResourceTypes, code);
    public static string Severity(string? code) => Lookup(MedLookups.NotificationSeverities, code);
    public static string Availability(string? code) => Lookup(MedLookups.AvailabilityStatuses, code);
    public static string OrderStatus(string? code) => Lookup(MedLookups.OrderStatuses, code);
    public static string Unit(string? code) => MedLookups.UnitCatalog.FirstOrDefault(u => Same(u.Code, code))?.Name ?? Fallback(code);

    public static string Action(string? code) => Lookup(MedLookups.ActionCodes, code) switch
    {
        var label when label != Fallback(code) => label,
        _ => code switch
        {
            "submit" => "Gửi duyệt",
            "restore" => "Khôi phục",
            "archive" => "Lưu trữ",
            "assign" => "Gán",
            "unassign" => "Gỡ gán",
            _ => Fallback(code)
        }
    };

    public static string Target(string? code) => code switch
    {
        "permission_change_request" => "Yêu cầu thay đổi quyền",
        "permission_change_item" => "Nội dung thay đổi quyền",
        "role_permission" => "Quyền của vai trò",
        "group_permission" => "Quyền của nhóm",
        "user_permission_override" => "Ghi đè quyền cá nhân",
        "user_role" => "Vai trò người dùng",
        "clinical_protocol" => "Phác đồ",
        "clinical_protocol_version" => "Phiên bản phác đồ",
        "clinical_protocol_procedure" => "Quy trình trong phác đồ",
        "protocol_applicability_rule" => "Điều kiện áp dụng phác đồ",
        "patient_protocol_application" => "Áp dụng phác đồ cho lượt khám",
        "professional_procedure" => "Quy trình kỹ thuật",
        "procedure_version" => "Phiên bản quy trình",
        "procedure_step" => "Bước quy trình",
        "procedure_screen_mapping" => "Liên kết màn hình quy trình",
        "technical_service" => "Dịch vụ kỹ thuật",
        "technical_order" => "Chỉ định kỹ thuật",
        "resource_catalog" => "Tài nguyên",
        "resource_availability_snapshot" => "Đối chiếu nguồn lực",
        "actual_resource_usage" => "Sử dụng nguồn lực thực tế",
        "department" or "departments" => "Khoa/phòng",
        "app_user" or "user" => "Người dùng",
        "role" => "Vai trò",
        "group" => "Nhóm",
        "notification" => "Thông báo",
        _ => Fallback(code)
    };

    public static string Module(string? code) => code switch
    {
        "CORE" => "Nền tảng",
        "ORG" => "Tổ chức",
        "PERM" => "Phân quyền",
        "PROC" => "Quy trình",
        "CAT" => "Danh mục kỹ thuật",
        "TECH" => "Điều phối kỹ thuật",
        "PROTOCOL" => "Phác đồ",
        "CLINICAL" => "Lâm sàng",
        "REPORT" => "Báo cáo",
        _ => Fallback(code)
    };

    public static string NotificationType(string? code) => code switch
    {
        "procedure_approval" => "Quy trình chờ phê duyệt",
        "procedure_published" => "Quy trình đã ban hành",
        "permission_change" => "Thay đổi quyền",
        "resource_warning" => "Cảnh báo nguồn lực",
        "clinical_protocol" => "Phác đồ lâm sàng",
        "protocol_approval" => "Phác đồ chờ duyệt",
        "order_resource" => "Nguồn lực chỉ định",
        _ => Fallback(code)
    };

    public static string Source(string? sourceType) => sourceType switch
    {
        "permission_change" => "Luồng phê duyệt quyền",
        "procedure" or "procedure_version" => "Quy trình kỹ thuật",
        "clinical_protocol" or "protocol" => "Phác đồ lâm sàng",
        "technical_order" or "order" => "Chỉ định kỹ thuật",
        "resource" or "inventory" => "Nguồn lực",
        _ => Fallback(sourceType)
    };

    public static string PermissionLabel(MedPermission? permission,
        IEnumerable<ScreenCatalog> screens, IEnumerable<FeatureCatalog> features)
    {
        if (permission is null) return Empty;
        var screen = screens.FirstOrDefault(s => s.ScreenId == permission.ScreenId)?.Name;
        var feature = permission.FeatureId.HasValue
            ? features.FirstOrDefault(f => f.FeatureId == permission.FeatureId.Value)?.Name
            : null;
        var action = Action(permission.ActionCode);
        return string.Join(" - ", new[] { screen, feature, action }.Where(v => !string.IsNullOrWhiteSpace(v)));
    }

    public static string ScreenLabel(ScreenCatalog? screen) => screen?.Name ?? Empty;
    public static string FeatureLabel(FeatureCatalog? feature) => feature?.Name ?? "Toàn màn hình";
    public static string ResourceLabel(ResourceCatalogItem? resource) => resource?.Name ?? Empty;

    public static IReadOnlyList<SqlDepartment> ActiveDepartmentsForSelection(IEnumerable<SqlDepartment> departments)
        => departments
            .Where(d => Same(d.Status, "active"))
            .OrderBy(d => d.Name)
            .ThenBy(d => d.Code)
            .ToList();

    public static string DepartmentLabel(SqlDepartment? department, IEnumerable<SqlDepartment> departments)
    {
        if (department is null) return Empty;

        var hasDuplicateName = departments.Any(d =>
            d.DepartmentId != department.DepartmentId &&
            Same(d.Status, department.Status) &&
            Same(d.Name, department.Name));
        return hasDuplicateName ? $"{department.Name} ({department.Code})" : department.Name;
    }

    public static string UnitGroup(string? unitCode)
        => MedLookups.UnitCatalog.FirstOrDefault(u => Same(u.Code, unitCode))?.UnitGroup ?? string.Empty;

    public static bool UnitsCompatible(string? expectedUnitCode, string? actualUnitCode)
    {
        var expectedGroup = UnitGroup(expectedUnitCode);
        var actualGroup = UnitGroup(actualUnitCode);
        return string.IsNullOrEmpty(expectedGroup) || string.IsNullOrEmpty(actualGroup) || expectedGroup == actualGroup;
    }

    public static IReadOnlyList<UnitEntry> CompatibleUnits(string? unitCode)
    {
        var group = UnitGroup(unitCode);
        return string.IsNullOrEmpty(group)
            ? MedLookups.UnitCatalog
            : MedLookups.UnitCatalog.Where(u => u.UnitGroup == group).ToList();
    }

    public static string JsonSummary(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Empty;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return "Có dữ liệu kỹ thuật";
            var parts = doc.RootElement.EnumerateObject()
                .Take(4)
                .Select(p => $"{JsonKey(p.Name)}: {JsonValue(p.Value)}")
                .Where(p => !p.EndsWith(": ", StringComparison.Ordinal));
            var summary = string.Join("; ", parts);
            return string.IsNullOrWhiteSpace(summary) ? "Có dữ liệu kỹ thuật" : summary;
        }
        catch (JsonException)
        {
            return "Có dữ liệu kỹ thuật";
        }
    }

    public static string NormalizeScope(string? scope) => Same(scope, "all") ? "global" : scope ?? string.Empty;
    public static string Fallback(string? code) => string.IsNullOrWhiteSpace(code) ? Empty : code.Replace('_', ' ');
    private static bool Same(string? left, string? right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private static string JsonKey(string key) => key switch
    {
        "source" => "Nguồn",
        "status" => "Trạng thái",
        "reason" => "Lý do",
        "changed" => "Nội dung",
        "before" => "Trước",
        "after" => "Sau",
        "permission" or "permissionCode" => "Quyền",
        _ => Fallback(key)
    };

    private static string JsonValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => Fallback(value.GetString()),
        JsonValueKind.Number => value.ToString(),
        JsonValueKind.True => "Có",
        JsonValueKind.False => "Không",
        JsonValueKind.Null => Empty,
        JsonValueKind.Object or JsonValueKind.Array => "xem chi tiết",
        _ => value.ToString()
    };
}
