namespace TelemedicineLandingPage.Models.Admin.Sql;

/// <summary>Phác đồ lâm sàng.</summary>
public sealed record ClinicalProtocol
{
    public Guid ClinicalProtocolId { get; init; } = Guid.NewGuid();
    public required string ProtocolCode { get; init; }
    public required string Name { get; init; }
    public required string ProtocolType { get; init; }
    public Guid? OwnerDepartmentId { get; init; }
    public string? Description { get; init; }
    public string Status { get; init; } = "active";
    public Guid? CreatedBy { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Phiên bản phác đồ lâm sàng.</summary>
public sealed record ClinicalProtocolVersion
{
    public Guid ClinicalProtocolVersionId { get; init; } = Guid.NewGuid();
    public required Guid ClinicalProtocolId { get; init; }
    public required int VersionNo { get; init; }
    public string StatusCode { get; init; } = "draft";
    public required string Title { get; init; }
    public string? Summary { get; init; }
    public string? ContentJson { get; init; }
    public DateTime? EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public Guid? CreatedBy { get; init; }
    public Guid? ApprovedBy { get; init; }
    public Guid? PublishedBy { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; init; }
    public DateTime? PublishedAt { get; init; }
}

/// <summary>Liên kết phác đồ với phiên bản quy trình.</summary>
public sealed record ClinicalProtocolProcedure
{
    public Guid ClinicalProtocolProcedureId { get; init; } = Guid.NewGuid();
    public required Guid ClinicalProtocolVersionId { get; init; }
    public required Guid ProcedureVersionId { get; init; }
    public string RelationType { get; init; } = "references";
    public int? SequenceNo { get; init; }
    public string? Note { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Quy tắc áp dụng phác đồ.</summary>
public sealed record ProtocolApplicabilityRule
{
    public Guid ProtocolApplicabilityRuleId { get; init; } = Guid.NewGuid();
    public required Guid ClinicalProtocolVersionId { get; init; }
    public required string RuleType { get; init; }
    public required string RuleJson { get; init; }
    public int Priority { get; init; } = 100;
    public bool IsActive { get; init; } = true;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Áp dụng phác đồ cho bệnh nhân.</summary>
public sealed record PatientProtocolApplication
{
    public Guid PatientProtocolApplicationId { get; init; } = Guid.NewGuid();
    public required Guid PatientRefId { get; init; }
    public Guid? EncounterRefId { get; init; }
    public string? DiagnosisCode { get; init; }
    public required Guid ClinicalProtocolVersionId { get; init; }
    public required string ApplicationStatus { get; init; }
    public Guid? AppliedBy { get; init; }
    public DateTime? AppliedAt { get; init; }
    public string? SkippedReason { get; init; }
    public string? DecisionContextJson { get; init; }
}
