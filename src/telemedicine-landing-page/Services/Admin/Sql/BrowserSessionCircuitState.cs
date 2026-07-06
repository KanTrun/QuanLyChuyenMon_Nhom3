namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>Per-Blazor-circuit session binding; avoids shared sessionStorage overwriting kick detection.</summary>
public sealed class BrowserSessionCircuitState
{
    public Guid? BoundSessionId { get; private set; }

    public void Bind(Guid sessionId)
    {
        if (sessionId == Guid.Empty)
        {
            return;
        }

        BoundSessionId = sessionId;
    }

    public void Clear()
        => BoundSessionId = null;

    public bool IsSupersededBy(Guid activeSessionId)
        => BoundSessionId is Guid bound && bound != activeSessionId;
}
