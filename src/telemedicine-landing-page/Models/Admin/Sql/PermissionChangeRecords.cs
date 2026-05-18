namespace TelemedicineLandingPage.Models.Admin.Sql;

/// <summary>Yêu cầu thay đổi quyền (mutually-exclusive Target* ids).</summary>
public sealed record PermissionChangeRequest
{
    public Guid PermissionChangeRequestId { get; init; } = Guid.NewGuid();
    public string ChangeStatus { get; init; } = "draft";
    public required string TargetType { get; init; }
    public Guid? TargetRoleId { get; init; }
    public Guid? TargetGroupId { get; init; }
    public Guid? TargetUserId { get; init; }
    public required string Reason { get; init; }
    public required Guid RequestedBy { get; init; }
    public Guid? ApprovedBy { get; init; }
    public Guid? AppliedBy { get; init; }
    public DateTime RequestedAt { get; init; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; init; }
    public required DateTime EffectiveAt { get; init; }
    public DateTime? AppliedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>Mục chi tiết trong yêu cầu thay đổi quyền.</summary>
public sealed record PermissionChangeItem
{
    public Guid PermissionChangeItemId { get; init; } = Guid.NewGuid();
    public required Guid PermissionChangeRequestId { get; init; }
    public required Guid PermissionId { get; init; }
    public required string OperationCode { get; init; }
    public required string EffectCode { get; init; }
    public required string DepartmentScopeType { get; init; }
    public Guid? DepartmentId { get; init; }
    public string? ScopeRuleJson { get; init; }
    public string? BeforeJson { get; init; }
    public string? AfterJson { get; init; }
    public DateTime? EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
}
