using TelemedicineLandingPage.Application.Workflow;
using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin.Sql;

public interface ITechnicalOrderWorkflowService
{
    void TransitionStatus(TechnicalOrder order, string targetStatus, Guid? actorUserId);
}

public sealed class TechnicalOrderWorkflowService : ITechnicalOrderWorkflowService
{
    private readonly IMedDataStore _store;
    private readonly AuditTrailService _audit;
    private readonly IWorkflowGuard<TechnicalOrder, string> _workflow;

    public TechnicalOrderWorkflowService(
        IMedDataStore store,
        AuditTrailService audit,
        IWorkflowGuard<TechnicalOrder, string> workflow)
    {
        _store = store;
        _audit = audit;
        _workflow = workflow;
    }

    public void TransitionStatus(TechnicalOrder order, string targetStatus, Guid? actorUserId)
    {
        if (!_workflow.CanTransition(order.OrderStatus, targetStatus))
        {
            throw MedDomainException.Constraint(
                "CK_technical_order_transition",
                50027,
                "Trang thai chi dinh ky thuat khong the chuyen truc tiep.");
        }

        var updated = order with
        {
            OrderStatus = targetStatus,
            CompletedAt = targetStatus == "completed" ? DateTime.UtcNow : order.CompletedAt
        };
        _store.UpdateTechnicalOrder(updated);
        _workflow.OnTransitioned(updated, order.OrderStatus, targetStatus, actorUserId);
        _audit.Append(new AuditLog
        {
            CorrelationId = Guid.NewGuid(),
            ActorUserId = actorUserId,
            ActionCode = ActionFor(targetStatus),
            TargetType = "technical_order",
            TargetId = order.TechnicalOrderId.ToString()
        });
    }

    private static string ActionFor(string status)
        => status switch
        {
            "completed" => "complete_order",
            "cancelled" => "cancel_order",
            _ => "update"
        };
}
