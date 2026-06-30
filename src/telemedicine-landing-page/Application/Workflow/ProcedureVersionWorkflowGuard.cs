using System.Security.Claims;
using System.Text.Json;
using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Application.Workflow;

public sealed class ProcedureVersionWorkflowGuard : IWorkflowGuard<ProcedureVersion, string>
{
    private static readonly WorkflowDefinition<string> Definition = new(new[]
    {
        ("draft", "pending_approval"),
        ("draft", "archived"),
        ("pending_approval", "active"),
        ("pending_approval", "rejected"),
        ("pending_approval", "archived"),
        ("rejected", "draft"),
        ("rejected", "archived"),
        ("active", "superseded"),
        ("active", "archived"),
        ("archived", "draft"),
        ("superseded", "active"),
        ("archived", "active")
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
