namespace TelemedicineLandingPage.Models.Admin.Sql;

/// <summary>Màn hình trong danh mục màn hình.</summary>
public sealed record ScreenCatalog
{
    public Guid ScreenId { get; init; } = Guid.NewGuid();
    public required string ScreenCode { get; init; }
    public required string Name { get; init; }
    public string? Route { get; init; }
    public string? ModuleCode { get; init; }
    public string Status { get; init; } = "active";
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Tính năng thuộc màn hình.</summary>
public sealed record FeatureCatalog
{
    public Guid FeatureId { get; init; } = Guid.NewGuid();
    public required Guid ScreenId { get; init; }
    public required string FeatureCode { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string Status { get; init; } = "active";
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Quyền trong hệ thống.</summary>
public sealed record MedPermission
{
    public Guid PermissionId { get; init; } = Guid.NewGuid();
    public required string PermissionCode { get; init; }
    public required Guid ScreenId { get; init; }
    public Guid? FeatureId { get; init; }
    public required string ActionCode { get; init; }
    public string? Description { get; init; }
    public string Status { get; init; } = "active";
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Gán quyền cho vai trò.</summary>
public sealed record RolePermission
{
    public Guid RolePermissionId { get; init; } = Guid.NewGuid();
    public required Guid RoleId { get; init; }
    public required Guid PermissionId { get; init; }
    public string EffectCode { get; init; } = "allow";
    public string DepartmentScopeType { get; init; } = "global";
    public Guid? DepartmentId { get; init; }
    public string? ScopeRuleJson { get; init; }
    public int Priority { get; init; } = 100;
    public string? Reason { get; init; }
    public DateTime EffectiveFrom { get; init; } = DateTime.UtcNow;
    public DateTime? EffectiveTo { get; init; }
    public Guid? CreatedBy { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Gán quyền cho nhóm.</summary>
public sealed record GroupPermission
{
    public Guid GroupPermissionId { get; init; } = Guid.NewGuid();
    public required Guid GroupId { get; init; }
    public required Guid PermissionId { get; init; }
    public string EffectCode { get; init; } = "allow";
    public string DepartmentScopeType { get; init; } = "global";
    public Guid? DepartmentId { get; init; }
    public string? ScopeRuleJson { get; init; }
    public int Priority { get; init; } = 200;
    public string? Reason { get; init; }
    public DateTime EffectiveFrom { get; init; } = DateTime.UtcNow;
    public DateTime? EffectiveTo { get; init; }
    public Guid? CreatedBy { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Ghi đè quyền cấp người dùng (deny-wins).</summary>
public sealed record UserPermissionOverride
{
    public Guid UserPermissionOverrideId { get; init; } = Guid.NewGuid();
    public required Guid UserId { get; init; }
    public required Guid PermissionId { get; init; }
    public required string EffectCode { get; init; }
    public string DepartmentScopeType { get; init; } = "global";
    public Guid? DepartmentId { get; init; }
    public string? ScopeRuleJson { get; init; }
    public int Priority { get; init; } = 300;
    public required string Reason { get; init; }
    public DateTime EffectiveFrom { get; init; } = DateTime.UtcNow;
    public DateTime? EffectiveTo { get; init; }
    public Guid? CreatedBy { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
