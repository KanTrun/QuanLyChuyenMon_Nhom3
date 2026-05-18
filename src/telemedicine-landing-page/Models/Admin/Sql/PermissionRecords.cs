using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TelemedicineLandingPage.Models.Admin.Sql;

/// <summary>Màn hình trong danh mục màn hình.</summary>
[Table("screen_catalog", Schema = "med")]
public sealed record ScreenCatalog
{
    [Key]
    [Column("screen_id")]
    public Guid ScreenId { get; init; } = Guid.NewGuid();

    [Column("screen_code")]
    public required string ScreenCode { get; init; }

    [Column("name")]
    public required string Name { get; init; }

    [Column("route")]
    public string? Route { get; init; }

    [Column("module_code")]
    public string? ModuleCode { get; init; }

    [Column("status")]
    public string Status { get; init; } = "active";

    [Column("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Tính năng thuộc màn hình.</summary>
[Table("feature_catalog", Schema = "med")]
public sealed record FeatureCatalog
{
    [Key]
    [Column("feature_id")]
    public Guid FeatureId { get; init; } = Guid.NewGuid();

    [Column("screen_id")]
    public required Guid ScreenId { get; init; }

    [Column("feature_code")]
    public required string FeatureCode { get; init; }

    [Column("name")]
    public required string Name { get; init; }

    [Column("description")]
    public string? Description { get; init; }

    [Column("status")]
    public string Status { get; init; } = "active";

    [Column("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Quyền trong hệ thống.</summary>
[Table("permissions", Schema = "med")]
public sealed record MedPermission
{
    [Key]
    [Column("permission_id")]
    public Guid PermissionId { get; init; } = Guid.NewGuid();

    [Column("permission_code")]
    public required string PermissionCode { get; init; }

    [Column("screen_id")]
    public required Guid ScreenId { get; init; }

    [Column("feature_id")]
    public Guid? FeatureId { get; init; }

    [Column("action_code")]
    public required string ActionCode { get; init; }

    [Column("description")]
    public string? Description { get; init; }

    [Column("status")]
    public string Status { get; init; } = "active";

    [Column("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Gán quyền cho vai trò.</summary>
[Table("role_permissions", Schema = "med")]
public sealed record RolePermission
{
    [Key]
    [Column("role_permission_id")]
    public Guid RolePermissionId { get; init; } = Guid.NewGuid();

    [Column("role_id")]
    public required Guid RoleId { get; init; }

    [Column("permission_id")]
    public required Guid PermissionId { get; init; }

    [Column("effect_code")]
    public string EffectCode { get; init; } = "allow";

    [Column("department_scope_type")]
    public string DepartmentScopeType { get; init; } = "global";

    [Column("department_id")]
    public Guid? DepartmentId { get; init; }

    [Column("scope_rule_json")]
    public string? ScopeRuleJson { get; init; }

    [Column("priority")]
    public int Priority { get; init; } = 100;

    [Column("reason")]
    public string? Reason { get; init; }

    [Column("effective_from")]
    public DateTime EffectiveFrom { get; init; } = DateTime.UtcNow;

    [Column("effective_to")]
    public DateTime? EffectiveTo { get; init; }

    [Column("created_by")]
    public Guid? CreatedBy { get; init; }

    [Column("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Gán quyền cho nhóm.</summary>
[Table("group_permissions", Schema = "med")]
public sealed record GroupPermission
{
    [Key]
    [Column("group_permission_id")]
    public Guid GroupPermissionId { get; init; } = Guid.NewGuid();

    [Column("group_id")]
    public required Guid GroupId { get; init; }

    [Column("permission_id")]
    public required Guid PermissionId { get; init; }

    [Column("effect_code")]
    public string EffectCode { get; init; } = "allow";

    [Column("department_scope_type")]
    public string DepartmentScopeType { get; init; } = "global";

    [Column("department_id")]
    public Guid? DepartmentId { get; init; }

    [Column("scope_rule_json")]
    public string? ScopeRuleJson { get; init; }

    [Column("priority")]
    public int Priority { get; init; } = 200;

    [Column("reason")]
    public string? Reason { get; init; }

    [Column("effective_from")]
    public DateTime EffectiveFrom { get; init; } = DateTime.UtcNow;

    [Column("effective_to")]
    public DateTime? EffectiveTo { get; init; }

    [Column("created_by")]
    public Guid? CreatedBy { get; init; }

    [Column("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Ghi đè quyền cấp người dùng (deny-wins).</summary>
[Table("user_permission_overrides", Schema = "med")]
public sealed record UserPermissionOverride
{
    [Key]
    [Column("user_permission_override_id")]
    public Guid UserPermissionOverrideId { get; init; } = Guid.NewGuid();

    [Column("user_id")]
    public required Guid UserId { get; init; }

    [Column("permission_id")]
    public required Guid PermissionId { get; init; }

    [Column("effect_code")]
    public required string EffectCode { get; init; }

    [Column("department_scope_type")]
    public string DepartmentScopeType { get; init; } = "global";

    [Column("department_id")]
    public Guid? DepartmentId { get; init; }

    [Column("scope_rule_json")]
    public string? ScopeRuleJson { get; init; }

    [Column("priority")]
    public int Priority { get; init; } = 300;

    [Column("reason")]
    public required string Reason { get; init; }

    [Column("effective_from")]
    public DateTime EffectiveFrom { get; init; } = DateTime.UtcNow;

    [Column("effective_to")]
    public DateTime? EffectiveTo { get; init; }

    [Column("created_by")]
    public Guid? CreatedBy { get; init; }

    [Column("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
