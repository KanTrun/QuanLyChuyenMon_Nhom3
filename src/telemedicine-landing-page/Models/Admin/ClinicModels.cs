namespace TelemedicineLandingPage.Models.Admin;

/// <summary>Status of a clinical session card on the Lâm sàng workboard.</summary>
public enum ClinicSessionStatus
{
    DangCho,
    DangThucHien,
    HoanThanh,
}

/// <summary>One simulated patient session on the clinical workboard.</summary>
public sealed record ClinicSession
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string PatientName { get; init; }
    public required string PatientCode { get; init; }
    public required Department Department { get; init; }
    public required string TechnicalService { get; init; }
    public required string AssignedTo { get; init; }
    public ClinicSessionStatus Status { get; init; } = ClinicSessionStatus.DangCho;
    public TimeOnly ScheduledAt { get; init; }
    public string Note { get; init; } = string.Empty;
}
