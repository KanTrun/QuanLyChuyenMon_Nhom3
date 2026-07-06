using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TelemedicineLandingPage.Models.Admin.Sql;

/// <summary>Phác đồ lâm sàng.</summary>
[Table("clinical_protocols", Schema = "med")]
public sealed record ClinicalProtocol
{
    [Key]
    [Column("clinical_protocol_id")]
    public Guid ClinicalProtocolId { get; init; } = Guid.NewGuid();

    [Column("protocol_code")]
    public required string ProtocolCode { get; init; }

    [Column("name")]
    public required string Name { get; init; }

    [Column("protocol_type")]
    public required string ProtocolType { get; init; }

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

/// <summary>Phiên bản phác đồ lâm sàng.</summary>
[Table("clinical_protocol_versions", Schema = "med")]
public sealed record ClinicalProtocolVersion
{
    [Key]
    [Column("clinical_protocol_version_id")]
    public Guid ClinicalProtocolVersionId { get; init; } = Guid.NewGuid();

    [Column("clinical_protocol_id")]
    public required Guid ClinicalProtocolId { get; init; }

    [Column("version_no")]
    public required int VersionNo { get; init; }

    [Column("status_code")]
    public string StatusCode { get; init; } = "draft";

    [Column("title")]
    public required string Title { get; init; }

    [Column("summary")]
    public string? Summary { get; init; }

    [Column("content_json")]
    public string? ContentJson { get; init; }

    [Column("effective_from")]
    public DateTime? EffectiveFrom { get; init; }

    [Column("effective_to")]
    public DateTime? EffectiveTo { get; init; }

    [Column("created_by")]
    public Guid? CreatedBy { get; init; }

    [Column("approved_by")]
    public Guid? ApprovedBy { get; init; }

    [Column("published_by")]
    public Guid? PublishedBy { get; init; }

    [Column("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    [Column("approved_at")]
    public DateTime? ApprovedAt { get; init; }

    [Column("published_at")]
    public DateTime? PublishedAt { get; init; }
}

/// <summary>Liên kết phác đồ với phiên bản quy trình.</summary>
[Table("clinical_protocol_procedures", Schema = "med")]
public sealed record ClinicalProtocolProcedure
{
    [Key]
    [Column("clinical_protocol_procedure_id")]
    public Guid ClinicalProtocolProcedureId { get; init; } = Guid.NewGuid();

    [Column("clinical_protocol_version_id")]
    public required Guid ClinicalProtocolVersionId { get; init; }

    [Column("procedure_version_id")]
    public required Guid ProcedureVersionId { get; init; }

    [Column("relation_type")]
    public string RelationType { get; init; } = "references";

    [Column("sequence_no")]
    public int? SequenceNo { get; init; }

    [Column("note")]
    public string? Note { get; init; }

    [Column("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Quy tắc áp dụng phác đồ.</summary>
[Table("protocol_applicability_rules", Schema = "med")]
public sealed record ProtocolApplicabilityRule
{
    [Key]
    [Column("protocol_applicability_rule_id")]
    public Guid ProtocolApplicabilityRuleId { get; init; } = Guid.NewGuid();

    [Column("clinical_protocol_version_id")]
    public required Guid ClinicalProtocolVersionId { get; init; }

    [Column("rule_type")]
    public required string RuleType { get; init; }

    [Column("rule_json")]
    public required string RuleJson { get; init; }

    [Column("priority")]
    public int Priority { get; init; } = 100;

    [Column("is_active")]
    public bool IsActive { get; init; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Áp dụng phác đồ cho bệnh nhân.</summary>
[Table("patient_protocol_applications", Schema = "med")]
public sealed record PatientProtocolApplication
{
    [Key]
    [Column("patient_protocol_application_id")]
    public Guid PatientProtocolApplicationId { get; init; } = Guid.NewGuid();

    [Column("patient_ref_id")]
    public required Guid PatientRefId { get; init; }

    [Column("encounter_ref_id")]
    public Guid? EncounterRefId { get; init; }

    [Column("diagnosis_code")]
    public string? DiagnosisCode { get; init; }

    [Column("clinical_protocol_version_id")]
    public required Guid ClinicalProtocolVersionId { get; init; }

    [Column("application_status")]
    public required string ApplicationStatus { get; init; }

    [Column("applied_by")]
    public Guid? AppliedBy { get; init; }

    [Column("applied_at")]
    public DateTime? AppliedAt { get; init; }

    [Column("skipped_reason")]
    public string? SkippedReason { get; init; }

    [Column("decision_context_json")]
    public string? DecisionContextJson { get; init; }
}
