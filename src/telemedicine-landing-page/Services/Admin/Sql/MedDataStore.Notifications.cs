using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>Phần quản lý thông báo, tùy chọn thông báo, lần thử gửi.</summary>
public sealed partial class MedDataStore
{
    public void AddNotificationPreference(NotificationPreference pref)
    {
        lock (_lock)
        {
            _notificationPrefs.Add(pref);
            RaiseStateChanged();
        }
    }

    public void AddNotification(MedNotification notification)
    {
        lock (_lock)
        {
            ValidateJson(notification.PayloadJson, "payload");
            _notifications.Add(notification);
            RaiseStateChanged();
        }
    }

    public void AddNotificationDeliveryAttempt(NotificationDeliveryAttempt attempt)
    {
        lock (_lock)
        {
            _deliveryAttempts.Add(attempt);
            RaiseStateChanged();
        }
    }
}
