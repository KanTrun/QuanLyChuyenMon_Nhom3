namespace TelemedicineLandingPage.Models.Admin.Sql;

/// <summary>Bản ghi nhật ký kiểm toán bất biến (immutable audit log).</summary>
public sealed record AuditLog
{
    public Guid AuditLogId { get; init; } = Guid.NewGuid();
    public long AuditLogSeq { get; init; }
    public required Guid CorrelationId { get; init; }
    public Guid? ActorUserId { get; init; }
    public string? ActorUsername { get; init; }
    public required string ActionCode { get; init; }
    public required string TargetType { get; init; }
    public string? TargetId { get; init; }
    public Guid? DepartmentId { get; init; }
    public string? BeforeJson { get; init; }
    public string? AfterJson { get; init; }
    public string? MetadataJson { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
