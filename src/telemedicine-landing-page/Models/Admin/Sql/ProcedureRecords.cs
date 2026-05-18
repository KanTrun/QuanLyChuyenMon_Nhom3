using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TelemedicineLandingPage.Models.Admin.Sql;

/// <summary>Quy trình chuyên môn.</summary>
[Table("professional_procedures", Schema = "med")]
public sealed record ProfessionalProcedure
{
    [Key]
    [Column("procedure_id")]
    public Guid ProcedureId { get; init; } = Guid.NewGuid();

    [Column("procedure_code")]
    public required string ProcedureCode { get; init; }

    [Column("name")]
    public required string Name { get; init; }

    [Column("procedure_type")]
    public required string ProcedureType { get; init; }

    [Column("owner_department_id")]
    public Guid? OwnerDepartmentId { get; init; }

    [Column("description")]
    public string? Description { get; init; }

    [Column("status")]
    public string Status { get; init; } = "active";

    [Column("created_by")]
    public Guid? CreatedBy { get; init; }

    [Column("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Phiên bản của quy trình.</summary>
[Table("procedure_versions", Schema = "med")]
public sealed record ProcedureVersion
{
    [Key]
    [Column("procedure_version_id")]
    public Guid ProcedureVersionId { get; init; } = Guid.NewGuid();

    [Column("procedure_id")]
    public required Guid ProcedureId { get; init; }

    [Column("version_no")]
    public required int VersionNo { get; init; }

    [Column("version_label")]
    public string? VersionLabel { get; init; }

    [Column("status_code")]
    public string StatusCode { get; init; } = "draft";

    [Column("department_id")]
    public Guid? DepartmentId { get; init; }

    [Column("title")]
    public required string Title { get; init; }

    [Column("summary")]
    public string? Summary { get; init; }

    [Column("change_reason")]
    public string? ChangeReason { get; init; }

    [Column("effective_from")]
    public DateTime? EffectiveFrom { get; init; }

    [Column("effective_to")]
    public DateTime? EffectiveTo { get; init; }

    [Column("created_by")]
    public Guid? CreatedBy { get; init; }

    [Column("submitted_by")]
    public Guid? SubmittedBy { get; init; }

    [Column("approved_by")]
    public Guid? ApprovedBy { get; init; }

    [Column("published_by")]
    public Guid? PublishedBy { get; init; }

    [Column("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    [Column("submitted_at")]
    public DateTime? SubmittedAt { get; init; }

    [Column("approved_at")]
    public DateTime? ApprovedAt { get; init; }

    [Column("published_at")]
    public DateTime? PublishedAt { get; init; }
}

/// <summary>Bước trong quy trình.</summary>
[Table("procedure_steps", Schema = "med")]
public sealed record ProcedureStep
{
    [Key]
    [Column("procedure_step_id")]
    public Guid ProcedureStepId { get; init; } = Guid.NewGuid();

    [Column("procedure_version_id")]
    public required Guid ProcedureVersionId { get; init; }

    [Column("step_no")]
    public required int StepNo { get; init; }

    [Column("step_code")]
    public string? StepCode { get; init; }

    [Column("name")]
    public required string Name { get; init; }

    [Column("description")]
    public string? Description { get; init; }

    [Column("actor_role_id")]
    public Guid? ActorRoleId { get; init; }

    [Column("transition_condition_json")]
    public string? TransitionConditionJson { get; init; }

    [Column("standard_duration_minutes")]
    public int? StandardDurationMinutes { get; init; }

    [Column("is_required")]
    public bool IsRequired { get; init; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Tài liệu đính kèm của phiên bản quy trình.</summary>
[Table("procedure_attachments", Schema = "med")]
public sealed record ProcedureAttachment
{
    [Key]
    [Column("procedure_attachment_id")]
    public Guid ProcedureAttachmentId { get; init; } = Guid.NewGuid();

    [Column("procedure_version_id")]
    public required Guid ProcedureVersionId { get; init; }

    [Column("attachment_type")]
    public string AttachmentType { get; init; } = "sop";

    [Column("file_name")]
    public required string FileName { get; init; }

    [Column("file_uri")]
    public required string FileUri { get; init; }

    [Column("mime_type")]
    public string? MimeType { get; init; }

    [Column("file_size_bytes")]
    public long? FileSizeBytes { get; init; }

    [Column("checksum_sha256")]
    public string? ChecksumSha256 { get; init; }

    [Column("uploaded_by")]
    public Guid? UploadedBy { get; init; }

    [Column("uploaded_at")]
    public DateTime UploadedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Ánh xạ quy trình với màn hình.</summary>
[Table("procedure_screen_mappings", Schema = "med")]
public sealed record ProcedureScreenMapping
{
    [Key]
    [Column("procedure_screen_mapping_id")]
    public Guid ProcedureScreenMappingId { get; init; } = Guid.NewGuid();

    [Column("procedure_version_id")]
    public required Guid ProcedureVersionId { get; init; }

    [Column("screen_id")]
    public required Guid ScreenId { get; init; }

    [Column("feature_id")]
    public Guid? FeatureId { get; init; }

    [Column("action_code")]
    public string? ActionCode { get; init; }

    [Column("enforcement_mode")]
    public string EnforcementMode { get; init; } = "warning";

    [Column("rule_json")]
    public string? RuleJson { get; init; }

    [Column("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
