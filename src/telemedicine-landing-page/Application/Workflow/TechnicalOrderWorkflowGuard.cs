using System.Security.Claims;
using System.Text.Json;
using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Application.Workflow;

public sealed class TechnicalOrderWorkflowGuard : IWorkflowGuard<TechnicalOrder, string>
{
    private static readonly WorkflowDefinition<string> Definition = new(new[]
    {
        ("ordered", "scheduled"),
        ("ordered", "cancelled"),
        ("scheduled", "in_progress"),
        ("scheduled", "cancelled"),
        ("in_progress", "completed"),
        ("in_progress", "cancelled")
    });

    private readonly AuditTrailService _audit;

    public TechnicalOrderWorkflowGuard(AuditTrailService audit)
    {
        _audit = audit;
    }

    public bool CanTransition(string currentState, string targetState, ClaimsPrincipal? user = null)
        => Definition.CanTransition(currentState, targetState);

    public IReadOnlyCollection<string> GetAllowedTransitions(string currentState, ClaimsPrincipal? user = null)
        => Definition.GetAllowedTransitions(currentState);

    public void OnTransitioned(
        TechnicalOrder entity,
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
            TargetType = "technical_order",
            TargetId = entity.TechnicalOrderId.ToString(),
            MetadataJson = JsonSerializer.Serialize(new
            {
                Workflow = "technical_order",
                FromState = fromState,
                ToState = toState,
                Reason = reason
            })
        });
    }
}
