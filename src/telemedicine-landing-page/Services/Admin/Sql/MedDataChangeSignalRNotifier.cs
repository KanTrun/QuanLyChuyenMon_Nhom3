using Microsoft.AspNetCore.SignalR;
using TelemedicineLandingPage.Hubs;

namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>
/// Fan-out datastore revision changes to every authenticated browser via SignalR.
/// </summary>
public sealed class MedDataChangeSignalRNotifier : IDisposable
{
    private readonly IMedDataChangeBus _changeBus;
    private readonly IHubContext<NotificationHub> _hub;

    public MedDataChangeSignalRNotifier(IMedDataChangeBus changeBus, IHubContext<NotificationHub> hub)
    {
        _changeBus = changeBus;
        _hub = hub;
        _changeBus.Changed += OnDataChanged;
    }

    private void OnDataChanged()
    {
        var revision = _changeBus.Revision;
        _ = _hub.Clients.Group(NotificationHub.DataSyncGroup)
            .SendAsync("DataRevisionChanged", revision);
    }

    public void Dispose() => _changeBus.Changed -= OnDataChanged;
}
