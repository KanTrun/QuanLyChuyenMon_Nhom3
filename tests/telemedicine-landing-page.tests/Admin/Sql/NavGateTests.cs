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
        Assert.False(gate.CanAccess("/qlcm/duong-dan-khong-ton-tai"));
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
        Assert.True(gate.CanAccess("/qlcm/bao-cao"));
        Assert.False(gate.CanAccess("/admin/phan-quyen"));
    }

    [Fact]
    public void CanAccess_ProcedureUpdateRouteRequiresUpdatePermission()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var viewerId = Guid.NewGuid();
        var editorId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        var procedureId = Guid.NewGuid();

        db.Users.AddRange(
            new AppUser
            {
                UserId = viewerId,
                Username = "procedure_viewer_only",
                FullName = "Procedure viewer",
                PrimaryDepartmentId = MedDataStoreSeed.DeptNoiId
            },
            new AppUser
            {
                UserId = editorId,
                Username = "procedure_editor",
                FullName = "Procedure editor",
                PrimaryDepartmentId = MedDataStoreSeed.DeptNoiId
            });
        db.Roles.Add(new Role { RoleId = roleId, Code = "PROC_EDITOR_TEST", Name = "Procedure editor" });
        db.UserRoles.Add(new UserRole { UserId = editorId, RoleId = roleId });
        db.Permissions.Add(new MedPermission
        {
            PermissionId = permissionId,
            PermissionCode = "SCR_PROCEDURES:UPDATE",
            ScreenId = MedDataStoreSeed.ScreenProcId,
            ActionCode = "update"
        });
        db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
        db.SaveChanges();

        var context = new CurrentUserContext(db, new EffectivePermissionResolver(db));
        var gate = new NavGate(context);
        var updateRoute = $"/qlcm/quy-trinh/{procedureId}/cap-nhat";

        context.SetCurrentUser(viewerId);
        Assert.False(gate.CanAccess(updateRoute));

        context.SetCurrentUser(editorId);
        Assert.True(gate.CanAccess(updateRoute));
    }

    [Fact]
    public void GetDisplayRoute_RewritesProfessionalAdminRouteForNonAdmin()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var userId = Guid.NewGuid();
        db.Users.Add(new AppUser
        {
            UserId = userId,
            Username = "business_user",
            FullName = "Nguoi dung nghiep vu",
            PrimaryDepartmentId = MedDataStoreSeed.DeptNoiId
        });
        db.SaveChanges();

        var context = new CurrentUserContext(db, new EffectivePermissionResolver(db));
        context.SetCurrentUser(userId);
        var gate = new NavGate(context);

        Assert.Equal("/qlcm/quy-trinh/tao?mode=draft", gate.GetDisplayRoute("/admin/quy-trinh/tao?mode=draft"));
        Assert.Equal("/admin/phan-quyen", gate.GetDisplayRoute("/admin/phan-quyen"));
    }

    [Fact]
    public void GetDisplayRoute_KeepsAdminRouteForSystemAdmin()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var context = new CurrentUserContext(db, new EffectivePermissionResolver(db));
        context.SetCurrentUser(MedDataStoreSeed.AdminUserId);
        var gate = new NavGate(context);

        Assert.Equal("/admin/quy-trinh/tao", gate.GetDisplayRoute("/admin/quy-trinh/tao"));
    }

    [Fact]
    public void Filter_RewritesProfessionalNavItemsForNonAdmin()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();

        db.Users.Add(new AppUser
        {
            UserId = userId,
            Username = "report_workspace_user",
            FullName = "Nguoi xem bao cao",
            PrimaryDepartmentId = MedDataStoreSeed.DeptNoiId
        });
        db.Roles.Add(new Role { RoleId = roleId, Code = "REPORT_WORKSPACE_TEST", Name = "Xem bao cao" });
        db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        db.Permissions.Add(new MedPermission
        {
            PermissionId = permissionId,
            PermissionCode = "SCR_REPORTS:VIEW",
            ScreenId = MedDataStoreSeed.ScreenDashId,
            ActionCode = "view"
        });
        db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
        db.SaveChanges();

        var context = new CurrentUserContext(db, new EffectivePermissionResolver(db));
        context.SetCurrentUser(userId);
        var gate = new NavGate(context);
        var nav = new[]
        {
            new AdminNavItem("Bao cao", "/admin/bao-cao", "chart", null),
        };

        var filtered = gate.Filter(nav);

        var item = Assert.Single(filtered);
        Assert.Equal("/qlcm/bao-cao", item.Url);
    }

    [Fact]
    public void GetFirstAccessibleRoute_UsesFirstAllowedChildRouteForLoginLanding()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();

        db.Users.Add(new AppUser
        {
            UserId = userId,
            Username = "procedure_creator",
            FullName = "Nguoi tao quy trinh",
            PrimaryDepartmentId = MedDataStoreSeed.DeptNoiId
        });
        db.Roles.Add(new Role { RoleId = roleId, Code = "PROC_CREATOR_TEST", Name = "Tao quy trinh" });
        db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        db.Permissions.Add(new MedPermission
        {
            PermissionId = permissionId,
            PermissionCode = "SCR_PROCEDURES:CREATE",
            ScreenId = MedDataStoreSeed.ScreenProcId,
            ActionCode = "create"
        });
        db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
        db.SaveChanges();

        var context = new CurrentUserContext(db, new EffectivePermissionResolver(db));
        context.SetCurrentUser(userId);
        var gate = new NavGate(context);
        var nav = new[]
        {
            new AdminNavItem("Tong quan", "/admin", "dashboard", null),
            new AdminNavItem("Quy trinh", "/admin/quy-trinh", "workflow", null, new List<AdminNavItem>
            {
                new("Danh sach", "/admin/quy-trinh", "list", null),
                new("Tao moi", "/admin/quy-trinh/tao", "plus", null),
            }),
        };

        var route = gate.GetFirstAccessibleRoute(nav);

        Assert.Equal("/qlcm/quy-trinh", route);
    }

    [Fact]
    public void CanAccess_AllowsFeatureLevelProcedureCreatePermission()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();

        db.Users.Add(new AppUser
        {
            UserId = userId,
            Username = "feature_proc_creator",
            FullName = "Nguoi tao quy trinh feature",
            PrimaryDepartmentId = MedDataStoreSeed.DeptNoiId
        });
        db.Roles.Add(new Role { RoleId = roleId, Code = "FEATURE_PROC_CREATOR_TEST", Name = "Tien si" });
        db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        db.Permissions.Add(new MedPermission
        {
            PermissionId = permissionId,
            PermissionCode = "SCR_PROCEDURE_CREATE:CREATE",
            ScreenId = MedDataStoreSeed.ScreenProcId,
            ActionCode = "create"
        });
        db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
        db.SaveChanges();

        var context = new CurrentUserContext(db, new EffectivePermissionResolver(db));
        context.SetCurrentUser(userId);
        var gate = new NavGate(context);

        Assert.True(gate.CanAccess("/admin/quy-trinh/tao"));
        Assert.True(gate.CanAccess("/admin/quy-trinh"));
        Assert.Equal("/qlcm/quy-trinh", gate.GetFirstAccessibleRoute(new[]
        {
            new AdminNavItem("Tong quan", "/admin", "dashboard", null),
            new AdminNavItem("Quy trinh", "/admin/quy-trinh", "workflow", null, new List<AdminNavItem>
            {
                new("Danh sach", "/admin/quy-trinh", "list", null),
                new("Tao moi", "/admin/quy-trinh/tao", "plus", null),
            }),
        }));
    }

    [Fact]
    public void CanAccess_AllowsDepartmentAssignedRoleWithoutExplicitContext()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();

        db.Users.Add(new AppUser
        {
            UserId = userId,
            Username = "dept_assigned_creator",
            FullName = "Nguoi tao theo khoa",
            PrimaryDepartmentId = MedDataStoreSeed.DeptNoiId
        });
        db.Roles.Add(new Role { RoleId = roleId, Code = "DEPT_PROC_CREATOR_TEST", Name = "Tao quy trinh theo khoa" });
        db.UserRoles.Add(new UserRole
        {
            UserId = userId,
            RoleId = roleId,
            DepartmentId = MedDataStoreSeed.DeptNoiId
        });
        db.Permissions.Add(new MedPermission
        {
            PermissionId = permissionId,
            PermissionCode = "SCR_PROCEDURES:CREATE",
            ScreenId = MedDataStoreSeed.ScreenProcId,
            ActionCode = "create"
        });
        db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
        db.SaveChanges();

        var context = new CurrentUserContext(db, new EffectivePermissionResolver(db));
        context.SetCurrentUser(userId);
        var gate = new NavGate(context);

        Assert.True(gate.CanAccess("/admin/quy-trinh/tao"));
        Assert.Equal("/qlcm/quy-trinh/tao", gate.GetFirstAccessibleRoute(new[]
        {
            new AdminNavItem("Quy trinh", "/admin/quy-trinh", "workflow", null, new List<AdminNavItem>
            {
                new("Tao moi", "/admin/quy-trinh/tao", "plus", null)
            })
        }));
    }

    [Fact]
    public void CanAccess_HonorsOwnDepartmentScopeWithoutExplicitContext()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();

        db.Users.Add(new AppUser
        {
            UserId = userId,
            Username = "own_dept_viewer",
            FullName = "Nguoi xem theo khoa",
            PrimaryDepartmentId = MedDataStoreSeed.DeptNoiId
        });
        db.Roles.Add(new Role { RoleId = roleId, Code = "OWN_DEPT_VIEWER_TEST", Name = "Xem theo khoa" });
        db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        db.Permissions.Add(new MedPermission
        {
            PermissionId = permissionId,
            PermissionCode = "SCR_REPORTS:VIEW",
            ScreenId = MedDataStoreSeed.ScreenDashId,
            ActionCode = "view"
        });
        db.RolePermissions.Add(new RolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId,
            DepartmentScopeType = "own_department"
        });
        db.SaveChanges();

        var context = new CurrentUserContext(db, new EffectivePermissionResolver(db));
        context.SetCurrentUser(userId);
        var gate = new NavGate(context);

        Assert.True(gate.CanAccess("/admin/bao-cao"));
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
