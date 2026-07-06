using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Services.Notifications;

public sealed class SignalRNotificationService : INotificationService
{
    private readonly IMedDataStore _store;
    private readonly INotificationRealtimePublisher _realtime;

    public SignalRNotificationService(IMedDataStore store, INotificationRealtimePublisher realtime)
    {
        _store = store;
        _realtime = realtime;
    }

    public async Task<MedNotification> SendToUserAsync(
        Guid userId,
        NotificationMessage notification,
        CancellationToken cancellationToken = default)
    {
        var persisted = Persist(userId, notification);
        await _realtime.SendToUserAsync(userId, ToEnvelope(persisted), cancellationToken);
        return persisted;
    }

    public async Task<IReadOnlyList<MedNotification>> SendToGroupAsync(
        string groupName,
        NotificationMessage notification,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(groupName))
        {
            throw new ArgumentException("Group name is required.", nameof(groupName));
        }

        var now = DateTime.UtcNow;
        var group = _store.Groups.FirstOrDefault(g =>
            string.Equals(g.Code, groupName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(g.Name, groupName, StringComparison.OrdinalIgnoreCase));

        if (group is null)
        {
            var hubOnly = ToEnvelope(NewNotification(Guid.Empty, notification));
            await _realtime.SendToGroupAsync(groupName, hubOnly, cancellationToken);
            return Array.Empty<MedNotification>();
        }

        var activeUserIds = _store.UserGroupMembers
            .Where(member => member.GroupId == group.GroupId
                && member.EffectiveFrom <= now
                && (member.EffectiveTo is null || member.EffectiveTo > now))
            .Select(member => member.UserId)
            .Distinct()
            .Where(IsActiveUser)
            .ToList();

        return await SendToUsersAsync(activeUserIds, notification, cancellationToken);
    }

    public async Task<IReadOnlyList<MedNotification>> BroadcastAsync(
        NotificationMessage notification,
        CancellationToken cancellationToken = default)
    {
        var activeUserIds = _store.Users
            .Where(u => string.Equals(u.Status, "active", StringComparison.OrdinalIgnoreCase) && u.DeletedAt is null)
            .Select(u => u.UserId)
            .Distinct()
            .ToList();

        return await SendToUsersAsync(activeUserIds, notification, cancellationToken);
    }

    private async Task<IReadOnlyList<MedNotification>> SendToUsersAsync(
        IReadOnlyCollection<Guid> userIds,
        NotificationMessage notification,
        CancellationToken cancellationToken)
    {
        var sent = new List<MedNotification>(userIds.Count);
        foreach (var userId in userIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            sent.Add(await SendToUserAsync(userId, notification, cancellationToken));
        }

        return sent;
    }

    private MedNotification Persist(Guid userId, NotificationMessage message)
    {
        var notification = NewNotification(userId, message);
        _store.AddNotification(notification);
        return notification;
    }

    private MedNotification NewNotification(Guid userId, NotificationMessage message)
        => new()
        {
            RecipientUserId = userId,
            NotificationType = message.NotificationType,
            Title = message.Title,
            Body = message.Body,
            Severity = message.Severity,
            SourceType = message.SourceType,
            SourceId = message.SourceId,
            PayloadJson = message.PayloadJson
        };

    private bool IsActiveUser(Guid userId)
        => _store.Users.Any(u => u.UserId == userId
            && string.Equals(u.Status, "active", StringComparison.OrdinalIgnoreCase)
            && u.DeletedAt is null);

    private static NotificationEnvelope ToEnvelope(MedNotification notification)
        => new(
            notification.NotificationId,
            notification.RecipientUserId == Guid.Empty ? null : notification.RecipientUserId,
            notification.NotificationType,
            notification.Title,
            notification.Body,
            notification.Severity,
            notification.CreatedAt);
}
