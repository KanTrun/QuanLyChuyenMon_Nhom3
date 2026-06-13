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

    [Column("issue_date")]
    public DateTime? IssueDate { get; init; }

    [Column("issue_number")]
    public int? IssueNumber { get; init; }

    [Column("source_pdf_file_name")]
    public string? SourcePdfFileName { get; init; }

    [Column("source_pdf_checksum_sha256")]
    public string? SourcePdfChecksumSha256 { get; init; }

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

    [Column("responsibility_text")]
    public string? ResponsibilityText { get; init; }

    [Column("flow_shape_code")]
    public string FlowShapeCode { get; init; } = "process";

    [Column("form_reference_text")]
    public string? FormReferenceText { get; init; }

    [Column("detail_section_number")]
    public string? DetailSectionNumber { get; init; }

    [Column("transition_condition_json")]
    public string? TransitionConditionJson { get; init; }

    [Column("standard_duration_minutes")]
    public int? StandardDurationMinutes { get; init; }

    [Column("is_required")]
    public bool IsRequired { get; init; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Muc lon cua van ban quy trinh y te.</summary>
[Table("procedure_document_sections", Schema = "med")]
public sealed record ProcedureDocumentSection
{
    [Key]
    [Column("procedure_document_section_id")]
    public Guid ProcedureDocumentSectionId { get; init; } = Guid.NewGuid();

    [Column("procedure_version_id")]
    public required Guid ProcedureVersionId { get; init; }

    [Column("section_order")]
    public required int SectionOrder { get; init; }

    [Column("section_number")]
    public required string SectionNumber { get; init; }

    [Column("title")]
    public required string Title { get; init; }

    [Column("section_kind")]
    public string SectionKind { get; init; } = "body";

    [Column("content_text")]
    public string? ContentText { get; init; }

    [Column("is_required")]
    public bool IsRequired { get; init; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Noi nhan ban hanh quy trinh.</summary>
[Table("procedure_distribution_recipients", Schema = "med")]
public sealed record ProcedureDistributionRecipient
{
    [Key]
    [Column("procedure_distribution_recipient_id")]
    public Guid ProcedureDistributionRecipientId { get; init; } = Guid.NewGuid();

    [Column("procedure_version_id")]
    public required Guid ProcedureVersionId { get; init; }

    [Column("display_order")]
    public required int DisplayOrder { get; init; }

    [Column("recipient_name")]
    public required string RecipientName { get; init; }

    [Column("is_marked")]
    public bool IsMarked { get; init; } = true;
}

/// <summary>Theo doi sua doi tai lieu.</summary>
[Table("procedure_revision_entries", Schema = "med")]
public sealed record ProcedureRevisionEntry
{
    [Key]
    [Column("procedure_revision_entry_id")]
    public Guid ProcedureRevisionEntryId { get; init; } = Guid.NewGuid();

    [Column("procedure_version_id")]
    public required Guid ProcedureVersionId { get; init; }

    [Column("display_order")]
    public required int DisplayOrder { get; init; }

    [Column("revision_date")]
    public DateTime? RevisionDate { get; init; }

    [Column("page_ref")]
    public string? PageRef { get; init; }

    [Column("section_ref")]
    public string? SectionRef { get; init; }

    [Column("summary")]
    public required string Summary { get; init; }
}

/// <summary>Ky xac nhan noi bo theo vai tro trong quy trinh.</summary>
[Table("procedure_signoff_records", Schema = "med")]
public sealed record ProcedureSignoffRecord
{
    [Key]
    [Column("procedure_signoff_record_id")]
    public Guid ProcedureSignoffRecordId { get; init; } = Guid.NewGuid();

    [Column("procedure_version_id")]
    public required Guid ProcedureVersionId { get; init; }

    [Column("signoff_role")]
    public required string SignoffRole { get; init; }

    [Column("display_order")]
    public int DisplayOrder { get; init; }

    [Column("signer_user_id")]
    public Guid? SignerUserId { get; init; }

    [Column("signer_username")]
    public string? SignerUsername { get; init; }

    [Column("signer_full_name")]
    public string? SignerFullName { get; init; }

    [Column("signed_at")]
    public DateTime SignedAt { get; init; } = DateTime.UtcNow;

    [Column("content_hash_sha256")]
    public required string ContentHashSha256 { get; init; }

    [Column("signature_image_data_url")]
    public string? SignatureImageDataUrl { get; init; }

    [Column("note")]
    public string? Note { get; init; }
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
