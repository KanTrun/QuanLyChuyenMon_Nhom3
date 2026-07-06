using System.Security.Claims;

namespace TelemedicineLandingPage.Application.Workflow;

public interface IWorkflowGuard<TEntity, TState>
    where TState : notnull
{
    bool CanTransition(TState currentState, TState targetState, ClaimsPrincipal? user = null);

    IReadOnlyCollection<TState> GetAllowedTransitions(TState currentState, ClaimsPrincipal? user = null);

    void OnTransitioned(TEntity entity, TState fromState, TState toState, Guid? actorUserId = null, string? reason = null);
}
