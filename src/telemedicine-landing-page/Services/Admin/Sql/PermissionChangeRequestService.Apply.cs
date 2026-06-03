using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin.Sql;

public sealed partial class PermissionChangeRequestService
{
    private void ApplyItems(PermissionChangeRequest req, Guid actorUserId, DateTime now)
    {
        var items = _db.PermissionChangeItems
            .Where(i => i.PermissionChangeRequestId == req.PermissionChangeRequestId)
            .ToList();
        if (items.Count == 0)
        {
            throw MedDomainException.Constraint("CK_permission_change_items_required", 50013,
                "Yêu cầu phải có ít nhất một mục thay đổi.");
        }

        foreach (var item in items)
        {
            var scope = NormalizeScope(item.DepartmentScopeType);
            var operation = item.OperationCode.ToLowerInvariant();
            if (req.TargetType == "role")
            {
                ApplyRolePermission(req, item, scope, operation, actorUserId, now);
            }
            else if (req.TargetType == "group")
            {
                ApplyGroupPermission(req, item, scope, operation, actorUserId, now);
            }
            else if (req.TargetType == "user")
            {
                ApplyUserPermission(req, item, scope, operation, actorUserId, now);
            }
            else
            {
                throw MedDomainException.Constraint("CK_permission_change_target_type", 50018,
                    "Loại đối tượng thay đổi quyền không hợp lệ.");
            }
        }
    }

    private void ApplyRolePermission(PermissionChangeRequest req, PermissionChangeItem item,
        string scope, string operation, Guid actorUserId, DateTime now)
    {
        var roleId = req.TargetRoleId
            ?? throw MissingTarget("vai trò");
        var existing = _db.RolePermissions.FirstOrDefault(p =>
            p.RoleId == roleId &&
            p.PermissionId == item.PermissionId &&
            p.DepartmentScopeType == scope &&
            p.DepartmentId == item.DepartmentId &&
            p.EffectiveTo == null);

        if (operation == "revoke")
        {
            Expire(existing, now);
            return;
        }

        if (existing is null)
        {
            _db.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = item.PermissionId,
                EffectCode = item.EffectCode,
                DepartmentScopeType = scope,
                DepartmentId = item.DepartmentId,
                ScopeRuleJson = item.ScopeRuleJson,
                Priority = 100,
                Reason = req.Reason,
                EffectiveFrom = now,
                CreatedBy = actorUserId
            });
            return;
        }

        _db.RolePermissions.Entry(existing).CurrentValues.SetValues(existing with
        {
            EffectCode = item.EffectCode,
            ScopeRuleJson = item.ScopeRuleJson,
            Reason = req.Reason
        });
    }

    private void ApplyGroupPermission(PermissionChangeRequest req, PermissionChangeItem item,
        string scope, string operation, Guid actorUserId, DateTime now)
    {
        var groupId = req.TargetGroupId
            ?? throw MissingTarget("nhóm");
        EnsureActiveGroup(groupId);
        var existing = _db.GroupPermissions.FirstOrDefault(p =>
            p.GroupId == groupId &&
            p.PermissionId == item.PermissionId &&
            p.DepartmentScopeType == scope &&
            p.DepartmentId == item.DepartmentId &&
            p.EffectiveTo == null);

        if (operation == "revoke")
        {
            Expire(existing, now);
            return;
        }

        if (existing is null)
        {
            _db.GroupPermissions.Add(new GroupPermission
            {
                GroupId = groupId,
                PermissionId = item.PermissionId,
                EffectCode = item.EffectCode,
                DepartmentScopeType = scope,
                DepartmentId = item.DepartmentId,
                ScopeRuleJson = item.ScopeRuleJson,
                Priority = 200,
                Reason = req.Reason,
                EffectiveFrom = now,
                CreatedBy = actorUserId
            });
            return;
        }

        _db.GroupPermissions.Entry(existing).CurrentValues.SetValues(existing with
        {
            EffectCode = item.EffectCode,
            ScopeRuleJson = item.ScopeRuleJson,
            Reason = req.Reason
        });
    }

    private void EnsureActiveGroup(Guid groupId)
    {
        var group = _db.Groups.FirstOrDefault(g => g.GroupId == groupId)
            ?? throw MissingTarget("nhóm");
        if (group.Status != "active")
        {
            throw MedDomainException.Constraint(
                "CK_groups_active_mutation",
                50022,
                "Nhom da luu tru khong cho phep thay doi thanh vien/quyen.");
        }
    }

    private void ApplyUserPermission(PermissionChangeRequest req, PermissionChangeItem item,
        string scope, string operation, Guid actorUserId, DateTime now)
    {
        var userId = req.TargetUserId
            ?? throw MissingTarget("người dùng");
        var existing = _db.UserPermissionOverrides.FirstOrDefault(p =>
            p.UserId == userId &&
            p.PermissionId == item.PermissionId &&
            p.DepartmentScopeType == scope &&
            p.DepartmentId == item.DepartmentId &&
            p.EffectiveTo == null);

        if (operation == "revoke")
        {
            Expire(existing, now);
            return;
        }

        if (existing is null)
        {
            _db.UserPermissionOverrides.Add(new UserPermissionOverride
            {
                UserId = userId,
                PermissionId = item.PermissionId,
                EffectCode = item.EffectCode,
                DepartmentScopeType = scope,
                DepartmentId = item.DepartmentId,
                ScopeRuleJson = item.ScopeRuleJson,
                Priority = 300,
                Reason = req.Reason,
                EffectiveFrom = now,
                CreatedBy = actorUserId
            });
            return;
        }

        _db.UserPermissionOverrides.Entry(existing).CurrentValues.SetValues(existing with
        {
            EffectCode = item.EffectCode,
            ScopeRuleJson = item.ScopeRuleJson,
            Reason = req.Reason
        });
    }

}
