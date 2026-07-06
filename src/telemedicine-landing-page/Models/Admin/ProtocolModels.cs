namespace TelemedicineLandingPage.Models.Admin;

/// <summary>Type of clinical protocol (chăm sóc / phẫu thuật / thủ thuật / điều trị).</summary>
public enum ProtocolType
{
    ChamSoc,
    PhauThuat,
    ThuThuat,
    DieuTri,
}

/// <summary>A single application of a protocol to a real patient (used for the application count).</summary>
public sealed record ProtocolApplication(
    Guid Id,
    Guid ProtocolId,
    string PatientName,
    string Outcome,
    DateTime AppliedAt);

/// <summary>A clinical protocol (phác đồ điều trị).</summary>
public sealed record ClinicalProtocolRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required ProtocolType ProtocolType { get; init; }
    public required Department Specialty { get; init; }
    public IReadOnlyList<string> IcdCodes { get; init; } = Array.Empty<string>();
    public string Contraindications { get; init; } = string.Empty;
    public CatalogStatus Status { get; init; } = CatalogStatus.HoatDong;
    public string Version { get; init; } = "1.0";
    public DateOnly EffectiveFrom { get; init; }
    public DateOnly? EffectiveTo { get; init; }
    public int ApplicationCount { get; init; }
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}
