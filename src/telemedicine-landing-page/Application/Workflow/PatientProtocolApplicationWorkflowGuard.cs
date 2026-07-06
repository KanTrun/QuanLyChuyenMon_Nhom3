using System.Security.Claims;
using System.Text.Json;
using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Application.Workflow;

public sealed class PatientProtocolApplicationWorkflowGuard
    : IWorkflowGuard<PatientProtocolApplication, string>
{
    private static readonly WorkflowDefinition<string> Definition = new(new[]
    {
        ("draft", "applied"),
        ("suggested", "applied"),
        ("applied", "signed"),
        ("applied", "revoked"),
        ("signed", "revoked")
    });

    private readonly AuditTrailService _audit;

    public PatientProtocolApplicationWorkflowGuard(AuditTrailService audit)
    {
        _audit = audit;
    }

    public bool CanTransition(string currentState, string targetState, ClaimsPrincipal? user = null)
    {
        if (targetState == "revoked" && user is not null)
        {
            var canRevoke = user.IsInRole("admin")
                || user.HasClaim("permission", "SCR_ADMIN:MANAGE_SIGNATURES");
            if (!canRevoke) return false;
        }

        return Definition.CanTransition(currentState, targetState);
    }

    public IReadOnlyCollection<string> GetAllowedTransitions(string currentState, ClaimsPrincipal? user = null)
        => Definition.GetAllowedTransitions(currentState);

    public void OnTransitioned(
        PatientProtocolApplication entity,
        string fromState,
        string toState,
        Guid? actorUserId = null,
        string? reason = null)
    {
        if (toState == "revoked" && string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Lý do thu hồi chữ ký là bắt buộc.");

        _audit.Append(new AuditLog
        {
            CorrelationId = Guid.NewGuid(),
            ActorUserId = actorUserId,
            ActionCode = toState == "signed" ? "sign" : "update",
            TargetType = "patient_protocol_application",
            TargetId = entity.PatientProtocolApplicationId.ToString(),
            MetadataJson = JsonSerializer.Serialize(new
            {
                Workflow = "patient_protocol_application",
                FromState = fromState,
                ToState = toState,
                Reason = reason
            })
        });
    }
}
