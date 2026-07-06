using System.Security.Claims;
using System.Text.Json;
using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Application.Workflow;

public sealed class ProcedureVersionWorkflowGuard : IWorkflowGuard<ProcedureVersion, string>
{
    private static readonly WorkflowDefinition<string> Definition = new(new[]
    {
        // Luồng chính: draft → pending_review → pending_approval → active
        ("draft", "pending_review"),          // Tất cả người viết ký xong → chờ kiểm tra
        ("pending_review", "pending_approval"), // Người kiểm tra ký → chờ phê duyệt
        ("pending_approval", "active"),        // Người phê duyệt ký & ban hành

        // Hoàn trả từ pending_review
        ("pending_review", "draft"),           // Kiểm tra hoàn trả về soạn thảo (cấp 1 hoặc cấp 2)

        // Hoàn trả từ pending_approval
        ("pending_approval", "pending_review"), // Phê duyệt hoàn trả về kiểm tra
        ("pending_approval", "draft"),          // Phê duyệt hoàn trả về soạn thảo (cấp 1 hoặc cấp 2)

        // Từ chối / lưu trữ
        ("pending_approval", "rejected"),
        ("pending_approval", "archived"),
        ("rejected", "draft"),
        ("rejected", "archived"),
        ("active", "superseded"),
        ("active", "archived"),
        ("archived", "draft"),
        ("superseded", "active"),
        ("archived", "active"),
        ("draft", "archived"),

        // Tương thích ngược: draft → pending_approval (cho dữ liệu cũ)
        ("draft", "pending_approval"),
    });

    private readonly AuditTrailService _audit;

    public ProcedureVersionWorkflowGuard(AuditTrailService audit)
    {
        _audit = audit;
    }

    public bool CanTransition(string currentState, string targetState, ClaimsPrincipal? user = null)
        => Definition.CanTransition(currentState, targetState);

    public IReadOnlyCollection<string> GetAllowedTransitions(string currentState, ClaimsPrincipal? user = null)
        => Definition.GetAllowedTransitions(currentState);

    public void OnTransitioned(
        ProcedureVersion entity,
        string fromState,
        string toState,
        Guid? actorUserId = null,
        string? reason = null)
    {
        _audit.Append(new AuditLog
        {
            CorrelationId = Guid.NewGuid(),
            ActorUserId = actorUserId,
            ActionCode = "update",
            TargetType = "procedure_version",
            TargetId = entity.ProcedureVersionId.ToString(),
            MetadataJson = JsonSerializer.Serialize(new
            {
                Event = "procedure_workflow",
                Workflow = "procedure_version",
                entity.ProcedureId,
                entity.ProcedureVersionId,
                entity.VersionLabel,
                VersionTitle = entity.Title,
                FromState = fromState,
                ToState = toState,
                Reason = reason
            })
        });
    }
}
