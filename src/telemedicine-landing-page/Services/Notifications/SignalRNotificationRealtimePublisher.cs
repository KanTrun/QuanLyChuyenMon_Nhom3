using Microsoft.AspNetCore.SignalR;
using TelemedicineLandingPage.Hubs;

namespace TelemedicineLandingPage.Services.Notifications;

public sealed class SignalRNotificationRealtimePublisher : INotificationRealtimePublisher
{
    private readonly IHubContext<NotificationHub> _hub;

    public SignalRNotificationRealtimePublisher(IHubContext<NotificationHub> hub)
    {
        _hub = hub;
    }

    public Task SendToUserAsync(Guid userId, NotificationEnvelope notification, CancellationToken cancellationToken)
        => _hub.Clients.Group(NotificationHub.UserGroup(userId))
            .SendAsync("ReceiveNotification", notification, cancellationToken);

    public Task SendToGroupAsync(string groupName, NotificationEnvelope notification, CancellationToken cancellationToken)
        => _hub.Clients.Group(NotificationHub.Group(groupName))
            .SendAsync("ReceiveNotification", notification, cancellationToken);
}
