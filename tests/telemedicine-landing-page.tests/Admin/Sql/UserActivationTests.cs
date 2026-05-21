using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Tests.Admin.Sql;

public sealed class UserActivationTests
{
    [Fact]
    public void InactiveUser_CanBeReactivatedAndLoadedIntoCurrentContext()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var userId = Guid.NewGuid();
        db.Users.Add(new AppUser
        {
            UserId = userId,
            Username = "reactivate_user",
            FullName = "Người dùng cần kích hoạt lại",
            Status = "inactive"
        });
        db.SaveChanges();

        var store = new MedDbDataStore(db);
        var context = new CurrentUserContext(db, new EffectivePermissionResolver(db));
        var inactiveUser = db.Users.First(u => u.UserId == userId);

        Assert.Throws<InvalidOperationException>(() => context.SetCurrentUser(userId));

        store.UpdateUser(inactiveUser with { Status = "active", UpdatedAt = DateTime.UtcNow });
        context.SetCurrentUser(userId);

        Assert.Equal(userId, context.CurrentUser?.UserId);
        Assert.Equal("active", context.CurrentUser?.Status);
    }
}
