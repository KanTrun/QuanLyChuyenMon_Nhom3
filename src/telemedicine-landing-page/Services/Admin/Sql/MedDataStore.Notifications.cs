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

    public void UpdateNotificationPreference(NotificationPreference pref)
    {
        lock (_lock)
        {
            var idx = _notificationPrefs.FindIndex(p => p.NotificationPreferenceId == pref.NotificationPreferenceId);
            if (idx < 0)
                throw MedDomainException.Constraint("PK_notification_preferences", 547, "Cài đặt thông báo không tồn tại.");
            _notificationPrefs[idx] = pref;
            RaiseStateChanged();
        }
    }

    public void RemoveNotificationPreference(Guid prefId)
    {
        lock (_lock)
        {
            var removed = _notificationPrefs.RemoveAll(p => p.NotificationPreferenceId == prefId);
            if (removed == 0)
                throw MedDomainException.Constraint("PK_notification_preferences", 547, "Cài đặt thông báo không tồn tại.");
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

    public void UpdateNotificationReadAt(Guid notificationId, DateTime readAt)
    {
        lock (_lock)
        {
            var idx = _notifications.FindIndex(n => n.NotificationId == notificationId);
            if (idx < 0)
                throw MedDomainException.Constraint("PK_notifications", 547, "Thông báo không tồn tại.");
            _notifications[idx] = _notifications[idx] with { ReadAt = readAt };
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
