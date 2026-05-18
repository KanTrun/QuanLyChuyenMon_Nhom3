namespace TelemedicineLandingPage.Models.Admin.Sql;

/// <summary>Quy trình chuyên môn.</summary>
public sealed record ProfessionalProcedure
{
    public Guid ProcedureId { get; init; } = Guid.NewGuid();
    public required string ProcedureCode { get; init; }
    public required string Name { get; init; }
    public required string ProcedureType { get; init; }
    public Guid? OwnerDepartmentId { get; init; }
    public string? Description { get; init; }
    public string Status { get; init; } = "active";
    public Guid? CreatedBy { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Phiên bản của quy trình.</summary>
public sealed record ProcedureVersion
{
    public Guid ProcedureVersionId { get; init; } = Guid.NewGuid();
    public required Guid ProcedureId { get; init; }
    public required int VersionNo { get; init; }
    public string? VersionLabel { get; init; }
    public string StatusCode { get; init; } = "draft";
    public Guid? DepartmentId { get; init; }
    public required string Title { get; init; }
    public string? Summary { get; init; }
    public string? ChangeReason { get; init; }
    public DateTime? EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public Guid? CreatedBy { get; init; }
    public Guid? SubmittedBy { get; init; }
    public Guid? ApprovedBy { get; init; }
    public Guid? PublishedBy { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public DateTime? PublishedAt { get; init; }
}

/// <summary>Bước trong quy trình.</summary>
public sealed record ProcedureStep
{
    public Guid ProcedureStepId { get; init; } = Guid.NewGuid();
    public required Guid ProcedureVersionId { get; init; }
    public required int StepNo { get; init; }
    public string? StepCode { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public Guid? ActorRoleId { get; init; }
    public string? TransitionConditionJson { get; init; }
    public int? StandardDurationMinutes { get; init; }
    public bool IsRequired { get; init; } = true;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Tài liệu đính kèm của phiên bản quy trình.</summary>
public sealed record ProcedureAttachment
{
    public Guid ProcedureAttachmentId { get; init; } = Guid.NewGuid();
    public required Guid ProcedureVersionId { get; init; }
    public string AttachmentType { get; init; } = "sop";
    public required string FileName { get; init; }
    public required string FileUri { get; init; }
    public string? MimeType { get; init; }
    public long? FileSizeBytes { get; init; }
    public string? ChecksumSha256 { get; init; }
    public Guid? UploadedBy { get; init; }
    public DateTime UploadedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Ánh xạ quy trình với màn hình.</summary>
public sealed record ProcedureScreenMapping
{
    public Guid ProcedureScreenMappingId { get; init; } = Guid.NewGuid();
    public required Guid ProcedureVersionId { get; init; }
    public required Guid ScreenId { get; init; }
    public Guid? FeatureId { get; init; }
    public string? ActionCode { get; init; }
    public string EnforcementMode { get; init; } = "warning";
    public string? RuleJson { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
