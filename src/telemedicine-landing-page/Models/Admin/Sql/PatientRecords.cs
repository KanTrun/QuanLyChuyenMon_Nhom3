namespace TelemedicineLandingPage.Models.Admin.Sql;

/// <summary>Tham chiếu bệnh nhân từ hệ thống ngoài.</summary>
public sealed record PatientRef
{
    public Guid PatientRefId { get; init; } = Guid.NewGuid();
    public required string ExternalPatientId { get; init; }
    public string SourceSystemCode { get; init; } = "default";
    public string? PatientCode { get; init; }
    public string? DisplayName { get; init; }
    public DateOnly? BirthDate { get; init; }
    public string? GenderCode { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Tham chiếu lượt khám/nhập viện từ hệ thống ngoài.</summary>
public sealed record EncounterRef
{
    public Guid EncounterRefId { get; init; } = Guid.NewGuid();
    public required Guid PatientRefId { get; init; }
    public required string ExternalEncounterId { get; init; }
    public string SourceSystemCode { get; init; } = "default";
    public string? EncounterType { get; init; }
    public Guid? DepartmentId { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? EndedAt { get; init; }
}
