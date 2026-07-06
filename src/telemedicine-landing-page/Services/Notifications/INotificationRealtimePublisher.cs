namespace TelemedicineLandingPage.Services.Notifications;

public interface INotificationRealtimePublisher
{
    Task SendToUserAsync(Guid userId, NotificationEnvelope notification, CancellationToken cancellationToken);

    Task SendToGroupAsync(string groupName, NotificationEnvelope notification, CancellationToken cancellationToken);
}
