using Microsoft.EntityFrameworkCore;
using TelemedicineLandingPage.Data;
using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>
/// Giải quyết quyền hiệu lực cho người dùng theo mô hình RBAC nhiều nguồn.
/// Tương đương view SQL vw_effective_user_permissions_source:
/// - Priority DESC (cao hơn ưu tiên hơn)
/// - deny-wins-on-tie: cùng priority thì deny thắng allow
/// - source_rank: user_override=3 > group=2 > role=1
/// </summary>
public sealed class EffectivePermissionResolver
{
    private readonly MedDbContext _db;

    public EffectivePermissionResolver(MedDbContext db)
    {
        _db = db;
    }

    /// <summary>Kết quả giải quyết quyền cho một permission cụ thể.</summary>
    public sealed record ResolvedPermission(
        Guid PermissionId,
        string PermissionCode,
        string EffectCode,
        int Priority,
        int SourceRank,
        string SourceType,
        string DepartmentScopeType,
        Guid? DepartmentId,
        DateTime EffectiveFrom);

    /// <summary>
    /// Lấy tất cả quyền hiệu lực của người dùng tại thời điểm hiện tại.
    /// </summary>
    public IReadOnlyList<ResolvedPermission> Resolve(Guid userId, Guid? contextDepartmentId = null)
    {
        var now = DateTime.UtcNow;
        var candidates = new List<ResolvedPermission>();
        var user = _db.Users.FirstOrDefault(u => u.UserId == userId && u.Status == "active" && u.DeletedAt == null);
        if (user is null)
        {
            return Array.Empty<ResolvedPermission>();
        }

        // 1. Quyền từ vai trò (source_rank = 1)
        var activeRoleAssignments = _db.UserRoles
            .Where(ur => ur.UserId == userId && ur.EffectiveFrom <= now && (ur.EffectiveTo == null || ur.EffectiveTo > now))
            .ToList();
        var activeRoles = activeRoleAssignments
            .Where(ur => AssignmentDepartmentApplies(ur.DepartmentId, contextDepartmentId))
            .Select(ur => ur.RoleId)
            .ToHashSet();

        var rolePermissions = _db.RolePermissions
            .Where(rp => activeRoles.Contains(rp.RoleId) && rp.EffectiveFrom <= now && (rp.EffectiveTo == null || rp.EffectiveTo > now))
            .AsEnumerable()
            .Where(rp => PermissionScopeApplies(rp.DepartmentScopeType, rp.DepartmentId, user.PrimaryDepartmentId, contextDepartmentId))
            .ToList();

        var permissionIds = rolePermissions.Select(rp => rp.PermissionId).ToHashSet();

        // 2. Quyền từ nhóm (source_rank = 2)
        var activeGroupIds = _db.UserGroupMembers
            .Where(gm => gm.UserId == userId && gm.EffectiveFrom <= now && (gm.EffectiveTo == null || gm.EffectiveTo > now))
            .Select(gm => gm.GroupId)
            .ToHashSet();
        var activeGroups = _db.Groups
            .Where(g => activeGroupIds.Contains(g.GroupId) && g.Status == "active")
            .AsEnumerable()
            .Where(g => AssignmentDepartmentApplies(g.DepartmentId, contextDepartmentId))
            .Select(g => g.GroupId)
            .ToHashSet();

        var groupPermissions = _db.GroupPermissions
            .Where(gp => activeGroups.Contains(gp.GroupId) && gp.EffectiveFrom <= now && (gp.EffectiveTo == null || gp.EffectiveTo > now))
            .AsEnumerable()
            .Where(gp => PermissionScopeApplies(gp.DepartmentScopeType, gp.DepartmentId, user.PrimaryDepartmentId, contextDepartmentId))
            .ToList();

        foreach (var gp in groupPermissions)
            permissionIds.Add(gp.PermissionId);

        // 3. Ghi đè cấp người dùng (source_rank = 3)
        var userOverrides = _db.UserPermissionOverrides
            .Where(upo => upo.UserId == userId && upo.EffectiveFrom <= now && (upo.EffectiveTo == null || upo.EffectiveTo > now))
            .AsEnumerable()
            .Where(upo => PermissionScopeApplies(upo.DepartmentScopeType, upo.DepartmentId, user.PrimaryDepartmentId, contextDepartmentId))
            .ToList();

        foreach (var upo in userOverrides)
            permissionIds.Add(upo.PermissionId);

        // Tải tất cả permissions liên quan
        var permissions = _db.Permissions
            .Where(p => permissionIds.Contains(p.PermissionId) && p.Status == "active")
            .ToDictionary(p => p.PermissionId);

        // Xây dựng danh sách ứng viên
        foreach (var rp in rolePermissions)
        {
            if (!permissions.TryGetValue(rp.PermissionId, out var perm)) continue;
            candidates.Add(new ResolvedPermission(
                rp.PermissionId, perm.PermissionCode, rp.EffectCode,
                rp.Priority, 1, "role", rp.DepartmentScopeType, rp.DepartmentId, rp.EffectiveFrom));
        }

        foreach (var gp in groupPermissions)
        {
            if (!permissions.TryGetValue(gp.PermissionId, out var perm)) continue;
            candidates.Add(new ResolvedPermission(
                gp.PermissionId, perm.PermissionCode, gp.EffectCode,
                gp.Priority, 2, "group", gp.DepartmentScopeType, gp.DepartmentId, gp.EffectiveFrom));
        }

        foreach (var upo in userOverrides)
        {
            if (!permissions.TryGetValue(upo.PermissionId, out var perm)) continue;
            candidates.Add(new ResolvedPermission(
                upo.PermissionId, perm.PermissionCode, upo.EffectCode,
                upo.Priority, 3, "user_override", upo.DepartmentScopeType, upo.DepartmentId, upo.EffectiveFrom));
        }

        // Giải quyết: nhóm theo PermissionId, chọn bản ghi thắng
        return candidates
            .GroupBy(c => c.PermissionId)
            .Select(PickWinner)
            .ToList();
    }

    /// <summary>
    /// Chọn bản ghi thắng trong nhóm cùng PermissionId:
    /// Khớp med.fn_user_has_permission_itvf:
    /// 1. priority cao nhất
    /// 2. deny thắng allow khi cùng priority
    /// 3. source_rank cao nhất (user_override > group > role)
    /// 4. effective_from mới nhất
    /// </summary>
    private static ResolvedPermission PickWinner(IGrouping<Guid, ResolvedPermission> group)
    {
        return group
            .OrderByDescending(c => c.Priority)
            .ThenByDescending(c => c.EffectCode == "deny")
            .ThenByDescending(c => c.SourceRank)
            .ThenByDescending(c => c.EffectiveFrom)
            .First();
    }

    private bool AssignmentDepartmentApplies(Guid? assignmentDepartmentId, Guid? contextDepartmentId)
    {
        if (assignmentDepartmentId is null) return true;
        if (contextDepartmentId is null) return false;
        if (assignmentDepartmentId == contextDepartmentId) return true;

        return _db.DepartmentClosure.Any(dc =>
            dc.AncestorDepartmentId == assignmentDepartmentId.Value &&
            dc.DescendantDepartmentId == contextDepartmentId.Value);
    }

    private bool PermissionScopeApplies(
        string scopeType,
        Guid? scopeDepartmentId,
        Guid? userPrimaryDepartmentId,
        Guid? contextDepartmentId)
    {
        return scopeType switch
        {
            "global" => true,
            "department" => contextDepartmentId.HasValue && scopeDepartmentId == contextDepartmentId,
            "department_tree" => contextDepartmentId.HasValue &&
                scopeDepartmentId.HasValue &&
                _db.DepartmentClosure.Any(dc =>
                    dc.AncestorDepartmentId == scopeDepartmentId.Value &&
                    dc.DescendantDepartmentId == contextDepartmentId.Value),
            "own_department" => contextDepartmentId.HasValue && userPrimaryDepartmentId == contextDepartmentId,
            _ => false,
        };
    }

    /// <summary>
    /// Kiểm tra nhanh người dùng có quyền cụ thể hay không.
    /// </summary>
    public bool HasPermission(Guid userId, string permissionCode, Guid? contextDepartmentId = null)
    {
        var resolved = Resolve(userId, contextDepartmentId);
        var match = resolved.FirstOrDefault(r => r.PermissionCode == permissionCode);
        return match is not null && match.EffectCode == "allow";
    }
}
