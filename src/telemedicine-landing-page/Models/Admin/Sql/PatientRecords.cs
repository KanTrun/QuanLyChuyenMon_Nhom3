using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TelemedicineLandingPage.Models.Admin.Sql;

/// <summary>Tham chiếu bệnh nhân từ hệ thống ngoài.</summary>
[Table("patient_refs", Schema = "med")]
public sealed record PatientRef
{
    [Key]
    [Column("patient_ref_id")]
    public Guid PatientRefId { get; init; } = Guid.NewGuid();

    [Column("external_patient_id")]
    public required string ExternalPatientId { get; init; }

    [Column("source_system_code")]
    public string SourceSystemCode { get; init; } = "default";

    [Column("patient_code")]
    public string? PatientCode { get; init; }

    [Column("display_name")]
    public string? DisplayName { get; init; }

    [Column("birth_date")]
    public DateOnly? BirthDate { get; init; }

    [Column("gender_code")]
    public string? GenderCode { get; init; }

    [Column("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Tham chiếu lượt khám/nhập viện từ hệ thống ngoài.</summary>
[Table("encounter_refs", Schema = "med")]
public sealed record EncounterRef
{
    [Key]
    [Column("encounter_ref_id")]
    public Guid EncounterRefId { get; init; } = Guid.NewGuid();

    [Column("patient_ref_id")]
    public required Guid PatientRefId { get; init; }

    [Column("external_encounter_id")]
    public required string ExternalEncounterId { get; init; }

    [Column("source_system_code")]
    public string SourceSystemCode { get; init; } = "default";

    [Column("encounter_type")]
    public string? EncounterType { get; init; }

    [Column("department_id")]
    public Guid? DepartmentId { get; init; }

    [Column("started_at")]
    public DateTime? StartedAt { get; init; }

    [Column("ended_at")]
    public DateTime? EndedAt { get; init; }
}
