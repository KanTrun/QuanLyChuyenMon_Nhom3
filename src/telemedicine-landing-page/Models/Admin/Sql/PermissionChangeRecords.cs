using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TelemedicineLandingPage.Models.Admin.Sql;

/// <summary>Yêu cầu thay đổi quyền (mutually-exclusive Target* ids).</summary>
[Table("permission_change_requests", Schema = "med")]
public sealed record PermissionChangeRequest
{
    [Key]
    [Column("permission_change_request_id")]
    public Guid PermissionChangeRequestId { get; init; } = Guid.NewGuid();

    [Column("change_status")]
    public string ChangeStatus { get; init; } = "draft";

    [Column("target_type")]
    public required string TargetType { get; init; }

    [Column("target_role_id")]
    public Guid? TargetRoleId { get; init; }

    [Column("target_group_id")]
    public Guid? TargetGroupId { get; init; }

    [Column("target_user_id")]
    public Guid? TargetUserId { get; init; }

    [Column("reason")]
    public required string Reason { get; init; }

    [Column("requested_by")]
    public required Guid RequestedBy { get; init; }

    [Column("approved_by")]
    public Guid? ApprovedBy { get; init; }

    [Column("applied_by")]
    public Guid? AppliedBy { get; init; }

    [Column("requested_at")]
    public DateTime RequestedAt { get; init; } = DateTime.UtcNow;

    [Column("approved_at")]
    public DateTime? ApprovedAt { get; init; }

    [Column("effective_at")]
    public required DateTime EffectiveAt { get; init; }

    [Column("applied_at")]
    public DateTime? AppliedAt { get; init; }

    [Column("error_message")]
    public string? ErrorMessage { get; init; }
}

/// <summary>Mục chi tiết trong yêu cầu thay đổi quyền.</summary>
[Table("permission_change_items", Schema = "med")]
public sealed record PermissionChangeItem
{
    [Key]
    [Column("permission_change_item_id")]
    public Guid PermissionChangeItemId { get; init; } = Guid.NewGuid();

    [Column("permission_change_request_id")]
    public required Guid PermissionChangeRequestId { get; init; }

    [Column("permission_id")]
    public required Guid PermissionId { get; init; }

    [Column("operation_code")]
    public required string OperationCode { get; init; }

    [Column("effect_code")]
    public required string EffectCode { get; init; }

    [Column("department_scope_type")]
    public required string DepartmentScopeType { get; init; }

    [Column("department_id")]
    public Guid? DepartmentId { get; init; }

    [Column("scope_rule_json")]
    public string? ScopeRuleJson { get; init; }

    [Column("before_json")]
    public string? BeforeJson { get; init; }

    [Column("after_json")]
    public string? AfterJson { get; init; }

    [Column("effective_from")]
    public DateTime? EffectiveFrom { get; init; }

    [Column("effective_to")]
    public DateTime? EffectiveTo { get; init; }
}
