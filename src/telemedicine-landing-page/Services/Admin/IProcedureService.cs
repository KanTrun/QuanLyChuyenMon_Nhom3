using TelemedicineLandingPage.Models.Admin;

namespace TelemedicineLandingPage.Services.Admin;

/// <summary>
/// In-memory CRUD service for clinical procedures (Quy trình kỹ thuật).
/// </summary>
public interface IProcedureService
{
    IReadOnlyList<ProcedureRecord> Search(ProcedureFilter filter);
    ProcedureRecord? GetById(Guid id);
    ProcedureRecord Create(ProcedureRecord record);
    ProcedureRecord Update(Guid id, ProcedureRecord updated);
    void Archive(Guid id, string actor);
    void SubmitForApproval(Guid id, string actor);
    void Approve(Guid id, string approver);
    void Reject(Guid id, string approver, string reason);

    event Action? StateChanged;
}
