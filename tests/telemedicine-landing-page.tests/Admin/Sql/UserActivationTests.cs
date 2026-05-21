using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Tests.Admin.Sql;

public sealed class UserActivationTests
{
    [Fact]
    public void SeededAdmin_CanLoginWithBootstrapPassword()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var context = new CurrentUserContext(db, new EffectivePermissionResolver(db));

        var result = context.LoginByUsernameDetailed(
            BootstrapAdminDefaults.Username,
            BootstrapAdminDefaults.LocalDevelopmentPassword);

        Assert.Equal(LoginAttemptStatus.Success, result.Status);
        Assert.Equal(BootstrapAdminDefaults.Username, result.User?.Username);
        Assert.Equal(result.User?.UserId, context.CurrentUser?.UserId);
    }

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

    [Fact]
    public void LoginByUsernameDetailed_ReturnsInactiveForRegisteredUserWaitingActivation()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var userId = Guid.NewGuid();
        db.Users.Add(new AppUser
        {
            UserId = userId,
            Username = "waiting_user",
            FullName = "Người dùng chờ kích hoạt",
            Email = "waiting@example.com",
            PasswordHash = CurrentUserContext.HashPassword("secret123"),
            Status = "inactive"
        });
        db.SaveChanges();

        var context = new CurrentUserContext(db, new EffectivePermissionResolver(db));

        var result = context.LoginByUsernameDetailed("waiting_user", "secret123");

        Assert.Equal(LoginAttemptStatus.Inactive, result.Status);
        Assert.Null(result.User);
        Assert.Null(context.CurrentUser);
    }

    [Fact]
    public void LoginByUsernameDetailed_DoesNotRevealInactiveStatusForWrongPassword()
    {
        using var db = TestDbHelper.CreateSeededContext();
        db.Users.Add(new AppUser
        {
            Username = "waiting_wrong_password",
            FullName = "Người dùng sai mật khẩu",
            PasswordHash = CurrentUserContext.HashPassword("secret123"),
            Status = "inactive"
        });
        db.SaveChanges();

        var context = new CurrentUserContext(db, new EffectivePermissionResolver(db));

        var result = context.LoginByUsernameDetailed("waiting_wrong_password", "wrongpass");

        Assert.Equal(LoginAttemptStatus.InvalidCredentials, result.Status);
        Assert.Null(result.User);
        Assert.Null(context.CurrentUser);
    }

    [Fact]
    public void LoginByUsernameDetailed_BlocksActiveAccountWithoutPassword()
    {
        using var db = TestDbHelper.CreateSeededContext();
        db.Users.Add(new AppUser
        {
            Username = "active_without_password",
            FullName = "Nguoi dung thieu mat khau",
            Status = "active"
        });
        db.SaveChanges();

        var context = new CurrentUserContext(db, new EffectivePermissionResolver(db));

        var result = context.LoginByUsernameDetailed("active_without_password", "anything");

        Assert.Equal(LoginAttemptStatus.PasswordNotSet, result.Status);
        Assert.Null(result.User);
        Assert.Null(context.CurrentUser);
    }
}
