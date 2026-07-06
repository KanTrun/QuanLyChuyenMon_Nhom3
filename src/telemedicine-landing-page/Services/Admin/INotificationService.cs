using TelemedicineLandingPage.Models.Admin;

namespace TelemedicineLandingPage.Services.Admin;

/// <summary>In-memory notification service. Drives the bell badge and flyout.</summary>
public interface INotificationService
{
    IReadOnlyList<Notification> ListAll();
    IReadOnlyList<Notification> ListUnread();
    int UnreadCount { get; }
    void MarkRead(Guid id);
    void MarkAllRead();
    Notification AddNotification(string title, string body, ActivitySeverity severity);

    event Action? StateChanged;
}
