using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Tests.Admin;

public sealed class AdminNavigationStateTests
{
    [Fact]
    public void NotificationBadge_UsesOnlyCurrentUsersSqlNotifications()
    {
        using var db = TelemedicineLandingPage.Tests.Admin.Sql.TestDbHelper.CreateSeededContext();
        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        db.Users.AddRange(
            new AppUser { UserId = currentUserId, Username = "current_user", FullName = "Người dùng hiện tại" },
            new AppUser { UserId = otherUserId, Username = "other_user", FullName = "Người dùng khác" });
        db.Notifications.AddRange(
            new MedNotification
            {
                RecipientUserId = currentUserId,
                NotificationType = "order_status",
                Title = "Thông báo của tôi",
                Body = "Nội dung",
                Severity = "info"
            },
            new MedNotification
            {
                RecipientUserId = otherUserId,
                NotificationType = "order_status",
                Title = "Thông báo người khác",
                Body = "Không được hiện",
                Severity = "info"
            });
        db.SaveChanges();

        var userContext = new CurrentUserContext(db, new EffectivePermissionResolver(db));
        userContext.SetCurrentUser(currentUserId);
        var state = new AdminNavigationState(new MedDbDataStore(db), userContext, new ThemeBus());

        Assert.Equal(1, state.UnreadNotifications);
        Assert.Single(state.NotificationPreviews);
        var preview = state.NotificationPreviews[0];
        Assert.Equal("Thông báo của tôi", preview.Title);
        Assert.True(preview.IsUnread);
        Assert.NotEqual(Guid.Empty, preview.NotificationId);
    }

    [Fact]
    public void MarkNotificationRead_UpdatesOnlyCurrentUsersNotification()
    {
        using var db = TelemedicineLandingPage.Tests.Admin.Sql.TestDbHelper.CreateSeededContext();
        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var currentNotificationId = Guid.NewGuid();
        var otherNotificationId = Guid.NewGuid();

        db.Users.AddRange(
            new AppUser { UserId = currentUserId, Username = "current_user", FullName = "Người dùng hiện tại" },
            new AppUser { UserId = otherUserId, Username = "other_user", FullName = "Người dùng khác" });
        db.Notifications.AddRange(
            new MedNotification
            {
                NotificationId = currentNotificationId,
                RecipientUserId = currentUserId,
                NotificationType = "order_status",
                Title = "Thông báo của tôi",
                Body = "Nội dung",
                Severity = "info"
            },
            new MedNotification
            {
                NotificationId = otherNotificationId,
                RecipientUserId = otherUserId,
                NotificationType = "order_status",
                Title = "Thông báo người khác",
                Body = "Không được cập nhật",
                Severity = "info"
            });
        db.SaveChanges();

        var userContext = new CurrentUserContext(db, new EffectivePermissionResolver(db));
        userContext.SetCurrentUser(currentUserId);
        var state = new AdminNavigationState(new MedDbDataStore(db), userContext, new ThemeBus());

        state.MarkNotificationRead(otherNotificationId);
        state.MarkNotificationRead(currentNotificationId);

        Assert.NotNull(db.Notifications.Single(n => n.NotificationId == currentNotificationId).ReadAt);
        Assert.Null(db.Notifications.Single(n => n.NotificationId == otherNotificationId).ReadAt);
        Assert.Equal(0, state.UnreadNotifications);
    }

    [Fact]
    public void StoreNotificationChanges_RaiseNavigationStateChanged()
    {
        using var db = TelemedicineLandingPage.Tests.Admin.Sql.TestDbHelper.CreateSeededContext();
        var userId = Guid.NewGuid();
        db.Users.Add(new AppUser { UserId = userId, Username = "current_user", FullName = "Người dùng hiện tại" });
        db.SaveChanges();

        var userContext = new CurrentUserContext(db, new EffectivePermissionResolver(db));
        userContext.SetCurrentUser(userId);
        var store = new MedDbDataStore(db);
        var state = new AdminNavigationState(store, userContext, new ThemeBus());
        var raised = false;
        state.StateChanged += () => raised = true;

        store.AddNotification(new MedNotification
        {
            RecipientUserId = userId,
            NotificationType = "order_status",
            Title = "Thông báo mới",
            Body = "Nội dung",
            Severity = "info"
        });

        Assert.True(raised);
        Assert.Equal(1, state.UnreadNotifications);
    }
}
