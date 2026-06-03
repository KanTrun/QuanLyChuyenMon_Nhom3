namespace TelemedicineLandingPage.Services.Admin.Sql;

public interface IMedDataChangeBus
{
    long Revision { get; }
    event Action? Changed;
    void Publish();
}

/// <summary>Broadcasts same-process datastore changes across Blazor circuits.</summary>
public sealed class MedDataChangeBus : IMedDataChangeBus
{
    private long _revision;

    public long Revision => Interlocked.Read(ref _revision);

    public event Action? Changed;

    public void Publish()
    {
        Interlocked.Increment(ref _revision);
        Changed?.Invoke();
    }
}
