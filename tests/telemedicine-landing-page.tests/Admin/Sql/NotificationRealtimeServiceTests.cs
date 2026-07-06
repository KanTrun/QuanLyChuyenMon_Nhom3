using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;
using TelemedicineLandingPage.Services.Notifications;

namespace TelemedicineLandingPage.Tests.Admin.Sql;

public sealed class NotificationRealtimeServiceTests
{
    [Fact]
    public async Task SendToUserAsync_PersistsNotificationAndPublishesEnvelope()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var userId = Guid.NewGuid();
        db.Users.Add(new AppUser
        {
            UserId = userId,
            Username = "realtime_user",
            FullName = "Realtime User"
        });
        db.SaveChanges();

        var publisher = new RecordingNotificationPublisher();
        var service = new SignalRNotificationService(new MedDbDataStore(db), publisher);

        var sent = await service.SendToUserAsync(userId, new NotificationMessage(
            "order_status",
            "Order updated",
            "Order moved to verified.",
            "info",
            "order",
            "ORD-1",
            "{\"orderId\":\"ORD-1\"}"));

        Assert.Contains(db.Notifications, n => n.NotificationId == sent.NotificationId);
        Assert.Single(publisher.UserMessages);
        Assert.Equal(userId, publisher.UserMessages[0].UserId);
        Assert.Equal(sent.NotificationId, publisher.UserMessages[0].Notification.NotificationId);
    }

    [Fact]
    public async Task BroadcastAsync_PersistsOnlyForActiveUsers()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var activeId = Guid.NewGuid();
        var inactiveId = Guid.NewGuid();
        var deletedId = Guid.NewGuid();
        db.Users.AddRange(
            new AppUser { UserId = activeId, Username = "active_user", FullName = "Active User", Status = "active" },
            new AppUser { UserId = inactiveId, Username = "inactive_user", FullName = "Inactive User", Status = "inactive" },
            new AppUser { UserId = deletedId, Username = "deleted_user", FullName = "Deleted User", Status = "active", DeletedAt = DateTime.UtcNow });
        db.SaveChanges();

        var publisher = new RecordingNotificationPublisher();
        var service = new SignalRNotificationService(new MedDbDataStore(db), publisher);

        var sent = await service.BroadcastAsync(new NotificationMessage(
            "system_broadcast",
            "Maintenance",
            "System maintenance starts soon.",
            "warning"));

        var activeRecipients = db.Users
            .Where(u => string.Equals(u.Status, "active", StringComparison.OrdinalIgnoreCase) && u.DeletedAt == null)
            .Select(u => u.UserId)
            .ToHashSet();

        Assert.Equal(activeRecipients.Count, sent.Count);
        Assert.Equal(activeRecipients.Count, publisher.UserMessages.Count);
        Assert.All(sent, n => Assert.Contains(n.RecipientUserId, activeRecipients));
        Assert.DoesNotContain(sent, n => n.RecipientUserId == inactiveId || n.RecipientUserId == deletedId);
    }

    [Fact]
    public async Task SendToGroupAsync_PersistsForCurrentActiveMembers()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var groupId = Guid.NewGuid();
        var activeMemberId = Guid.NewGuid();
        var expiredMemberId = Guid.NewGuid();
        var inactiveMemberId = Guid.NewGuid();
        db.Groups.Add(new Group { GroupId = groupId, Code = "LAB_TEAM", Name = "Lab Team" });
        db.Users.AddRange(
            new AppUser { UserId = activeMemberId, Username = "lab_active", FullName = "Lab Active", Status = "active" },
            new AppUser { UserId = expiredMemberId, Username = "lab_expired", FullName = "Lab Expired", Status = "active" },
            new AppUser { UserId = inactiveMemberId, Username = "lab_inactive", FullName = "Lab Inactive", Status = "inactive" });
        db.UserGroupMembers.AddRange(
            new UserGroupMember { GroupId = groupId, UserId = activeMemberId, EffectiveFrom = DateTime.UtcNow.AddDays(-1) },
            new UserGroupMember { GroupId = groupId, UserId = expiredMemberId, EffectiveFrom = DateTime.UtcNow.AddDays(-3), EffectiveTo = DateTime.UtcNow.AddDays(-2) },
            new UserGroupMember { GroupId = groupId, UserId = inactiveMemberId, EffectiveFrom = DateTime.UtcNow.AddDays(-1) });
        db.SaveChanges();

        var publisher = new RecordingNotificationPublisher();
        var service = new SignalRNotificationService(new MedDbDataStore(db), publisher);

        var sent = await service.SendToGroupAsync("LAB_TEAM", new NotificationMessage(
            "lab_notice",
            "Lab notice",
            "A new lab rule was published."));

        var notification = Assert.Single(sent);
        Assert.Equal(activeMemberId, notification.RecipientUserId);
        Assert.Single(publisher.UserMessages);
        Assert.Equal(activeMemberId, publisher.UserMessages[0].UserId);
    }

    private sealed class RecordingNotificationPublisher : INotificationRealtimePublisher
    {
        public List<(Guid UserId, NotificationEnvelope Notification)> UserMessages { get; } = new();

        public List<(string GroupName, NotificationEnvelope Notification)> GroupMessages { get; } = new();

        public Task SendToUserAsync(Guid userId, NotificationEnvelope notification, CancellationToken cancellationToken)
        {
            UserMessages.Add((userId, notification));
            return Task.CompletedTask;
        }

        public Task SendToGroupAsync(string groupName, NotificationEnvelope notification, CancellationToken cancellationToken)
        {
            GroupMessages.Add((groupName, notification));
            return Task.CompletedTask;
        }
    }
}
