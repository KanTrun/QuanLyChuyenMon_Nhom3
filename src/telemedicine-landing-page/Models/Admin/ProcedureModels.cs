namespace TelemedicineLandingPage.Models.Admin;

/// <summary>Lifecycle status of a clinical procedure.</summary>
public enum ProcedureStatus
{
    DangSoanThao,
    DangChoPheDuyet,
    DaBanHanh,
    NgungSuDung,
}

/// <summary>A single step within a procedure (sequence, name, actor role, expected duration).</summary>
public sealed record ProcedureStep(
    int Sequence,
    string Name,
    string ActorRole,
    int StandardMinutes,
    string TransitionCondition);

/// <summary>A clinical procedure record (Quy trình kỹ thuật).</summary>
public sealed record ProcedureRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required Department Department { get; init; }
    public required string Version { get; init; }
    public required ProcedureStatus Status { get; init; }
    public DateOnly EffectiveFrom { get; init; }
    public DateOnly? EffectiveTo { get; init; }
    public IReadOnlyList<ProcedureStep> Steps { get; init; } = Array.Empty<ProcedureStep>();
    public DateTime UpdatedAt { get; init; } = DateTime.Now;
    public string UpdatedBy { get; init; } = string.Empty;
    public string? RejectionReason { get; init; }
}

/// <summary>Filter object for procedure search.</summary>
public sealed record ProcedureFilter(
    string? Search = null,
    ProcedureStatus? Status = null,
    Department? Department = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null);
