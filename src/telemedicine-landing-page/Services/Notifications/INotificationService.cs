using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Notifications;

public interface INotificationService
{
    Task<MedNotification> SendToUserAsync(Guid userId, NotificationMessage notification, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MedNotification>> SendToGroupAsync(string groupName, NotificationMessage notification, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MedNotification>> BroadcastAsync(NotificationMessage notification, CancellationToken cancellationToken = default);
}
