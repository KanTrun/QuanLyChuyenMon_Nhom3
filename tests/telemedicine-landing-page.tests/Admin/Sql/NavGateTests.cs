using TelemedicineLandingPage.Models.Admin;
using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Tests.Admin.Sql;

public sealed class NavGateTests
{
    [Fact]
    public void CanAccess_DeniesDirectRouteWithoutPermission()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var userId = Guid.NewGuid();
        db.Users.Add(new AppUser
        {
            UserId = userId,
            Username = "no_permission_user",
            FullName = "Người dùng không có quyền",
            PrimaryDepartmentId = MedDataStoreSeed.DeptNoiId
        });
        db.SaveChanges();

        var context = new CurrentUserContext(db, new EffectivePermissionResolver(db));
        context.SetCurrentUser(userId);
        var gate = new NavGate(context);

        Assert.False(gate.CanAccess("/admin/phan-quyen"));
    }

    [Fact]
    public void CanAccess_DeniesUnknownAdminRouteForNonAdmin()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var userId = Guid.NewGuid();
        db.Users.Add(new AppUser
        {
            UserId = userId,
            Username = "unknown_admin_route_user",
            FullName = "Nguoi dung khong co quyen admin",
            PrimaryDepartmentId = MedDataStoreSeed.DeptNoiId
        });
        db.SaveChanges();

        var context = new CurrentUserContext(db, new EffectivePermissionResolver(db));
        context.SetCurrentUser(userId);
        var gate = new NavGate(context);

        Assert.False(gate.CanAccess("/admin/duong-dan-khong-ton-tai"));
        Assert.True(gate.CanAccess("/workspace-khong-bao-ve"));
    }

    [Fact]
    public void CanAccess_AllowsRouteWhenSqlPermissionMatchesCaseInsensitive()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();

        db.Users.Add(new AppUser
        {
            UserId = userId,
            Username = "report_viewer",
            FullName = "Người xem báo cáo",
            PrimaryDepartmentId = MedDataStoreSeed.DeptNoiId
        });
        db.Roles.Add(new Role { RoleId = roleId, Code = "REPORT_VIEWER_TEST", Name = "Xem báo cáo" });
        db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        db.Permissions.Add(new MedPermission
        {
            PermissionId = permissionId,
            PermissionCode = "scr_reports:view",
            ScreenId = MedDataStoreSeed.ScreenDashId,
            ActionCode = "view"
        });
        db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
        db.SaveChanges();

        var context = new CurrentUserContext(db, new EffectivePermissionResolver(db));
        context.SetCurrentUser(userId);
        var gate = new NavGate(context);

        Assert.True(gate.CanAccess("/admin/bao-cao"));
        Assert.False(gate.CanAccess("/admin/phan-quyen"));
    }

    [Fact]
    public void Filter_RemovesNavItemsTheUserCannotOpen()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var userId = Guid.NewGuid();
        db.Users.Add(new AppUser
        {
            UserId = userId,
            Username = "limited_user",
            FullName = "Người dùng giới hạn",
            PrimaryDepartmentId = MedDataStoreSeed.DeptNoiId
        });
        db.SaveChanges();

        var context = new CurrentUserContext(db, new EffectivePermissionResolver(db));
        context.SetCurrentUser(userId);
        var gate = new NavGate(context);
        var nav = new[]
        {
            new AdminNavItem("Phân quyền", "/admin/phan-quyen", "shield", null),
            new AdminNavItem("Không bảo vệ", "/khong-bao-ve", "info", null),
        };

        var filtered = gate.Filter(nav);

        Assert.DoesNotContain(filtered, item => item.Url == "/admin/phan-quyen");
        Assert.Contains(filtered, item => item.Url == "/khong-bao-ve");
    }
}
