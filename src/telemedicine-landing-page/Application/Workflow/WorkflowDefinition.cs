namespace TelemedicineLandingPage.Application.Workflow;

public sealed class WorkflowDefinition<TState>
    where TState : notnull
{
    private readonly Dictionary<TState, HashSet<TState>> _transitions;

    public WorkflowDefinition(IEnumerable<(TState From, TState To)> transitions)
    {
        _transitions = transitions
            .GroupBy(transition => transition.From)
            .ToDictionary(
                group => group.Key,
                group => group.Select(transition => transition.To).ToHashSet());
    }

    public bool CanTransition(TState currentState, TState targetState)
        => _transitions.TryGetValue(currentState, out var targets) && targets.Contains(targetState);

    public IReadOnlyCollection<TState> GetAllowedTransitions(TState currentState)
        => _transitions.TryGetValue(currentState, out var targets)
            ? targets.ToArray()
            : Array.Empty<TState>();
}
