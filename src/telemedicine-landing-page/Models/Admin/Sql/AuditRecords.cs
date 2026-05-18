using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TelemedicineLandingPage.Models.Admin.Sql;

/// <summary>Bản ghi nhật ký kiểm toán bất biến (immutable audit log).</summary>
[Table("audit_logs", Schema = "med")]
public sealed record AuditLog
{
    [Key]
    [Column("audit_log_id")]
    public Guid AuditLogId { get; init; } = Guid.NewGuid();

    [Column("audit_log_seq")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long AuditLogSeq { get; init; }

    [Column("correlation_id")]
    public required Guid CorrelationId { get; init; }

    [Column("actor_user_id")]
    public Guid? ActorUserId { get; init; }

    [Column("actor_username")]
    public string? ActorUsername { get; init; }

    [Column("action_code")]
    public required string ActionCode { get; init; }

    [Column("target_type")]
    public required string TargetType { get; init; }

    [Column("target_id")]
    public string? TargetId { get; init; }

    [Column("department_id")]
    public Guid? DepartmentId { get; init; }

    [Column("before_json")]
    public string? BeforeJson { get; init; }

    [Column("after_json")]
    public string? AfterJson { get; init; }

    [Column("metadata_json")]
    public string? MetadataJson { get; init; }

    [Column("ip_address")]
    public string? IpAddress { get; init; }

    [Column("user_agent")]
    public string? UserAgent { get; init; }

    [Column("occurred_at")]
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
