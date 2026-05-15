using TelemedicineLandingPage.Models.Admin;
using TelemedicineLandingPage.Services.Admin;

namespace TelemedicineLandingPage.Tests.Admin;

public sealed class PermissionServiceTests
{
    [Fact]
    public void UpdateRolePermissions_RecordsChangeLog()
    {
        var service = new PermissionService();
        var role = service.ListRoles().First(r => r.Code == "BSDT");

        var beforeLog = service.GetChangeLog().Count;
        var grants = service.AdminModules
            .Select(m => new PermissionGrant(m, true, true, true, false, false))
            .ToList();

        var raised = false;
        service.StateChanged += () => raised = true;

        service.UpdateRolePermissions(
            role.Id,
            grants,
            reason: "Cập nhật quyền cho ca trực mới",
            effectiveAt: DateTime.Now,
            changedBy: "BS. Đặng Thái Sơn");

        Assert.True(raised);
        var afterLog = service.GetChangeLog(role.Id);
        Assert.Single(afterLog);
        Assert.Equal(beforeLog + 1, service.GetChangeLog().Count);

        var entry = afterLog[0];
        Assert.Equal(PermissionTargetType.Role, entry.TargetType);
        Assert.Equal("Cập nhật quyền cho ca trực mới", entry.Reason);
        Assert.Equal("BS. Đặng Thái Sơn", entry.ChangedBy);
        Assert.False(string.IsNullOrWhiteSpace(entry.BeforeJson));
        Assert.False(string.IsNullOrWhiteSpace(entry.AfterJson));
        Assert.NotEqual(entry.BeforeJson, entry.AfterJson);

        var refreshed = service.GetRole(role.Id);
        Assert.NotNull(refreshed);
        Assert.All(refreshed!.Permissions, g => Assert.True(g.CanCreate));
    }

    [Fact]
    public void AssignUserRoles_PersistsAndRaises()
    {
        var service = new PermissionService();
        var roles = service.ListRoles();
        var user = service.ListUsers().First();
        var newRoles = roles.Select(r => r.Id).Take(2).ToList();

        var raised = false;
        service.StateChanged += () => raised = true;

        service.AssignUserRoles(user.Id, newRoles, reason: "Gán quyền tạm thời", changedBy: "Hệ thống");

        Assert.True(raised);
        var refreshed = service.ListUsers().First(u => u.Id == user.Id);
        Assert.Equal(newRoles.Count, refreshed.RoleIds.Count);
        Assert.True(newRoles.All(refreshed.RoleIds.Contains));

        var log = service.GetChangeLog(user.Id);
        Assert.NotEmpty(log);
        Assert.Equal(PermissionTargetType.User, log[0].TargetType);
        Assert.Equal("Gán quyền tạm thời", log[0].Reason);
    }
}
