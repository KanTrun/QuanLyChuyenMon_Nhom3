using Microsoft.EntityFrameworkCore;
using TelemedicineLandingPage.Data;
using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>
/// Giải quyết quyền hiệu lực cho người dùng theo mô hình RBAC nhiều nguồn.
/// Tương đương view SQL vw_effective_user_permissions_source:
/// - source_rank: user_override=3 > group=2 > role=1
/// - Priority DESC (cao hơn ưu tiên hơn)
/// - deny-wins-on-tie: cùng priority thì deny thắng allow
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
        Guid? DepartmentId);

    /// <summary>
    /// Lấy tất cả quyền hiệu lực của người dùng tại thời điểm hiện tại.
    /// </summary>
    public IReadOnlyList<ResolvedPermission> Resolve(Guid userId)
    {
        var now = DateTime.UtcNow;
        var candidates = new List<ResolvedPermission>();

        // 1. Quyền từ vai trò (source_rank = 1)
        var activeRoles = _db.UserRoles
            .Where(ur => ur.UserId == userId && ur.EffectiveFrom <= now && (ur.EffectiveTo == null || ur.EffectiveTo > now))
            .Select(ur => ur.RoleId)
            .ToHashSet();

        var rolePermissions = _db.RolePermissions
            .Where(rp => activeRoles.Contains(rp.RoleId) && rp.EffectiveFrom <= now && (rp.EffectiveTo == null || rp.EffectiveTo > now))
            .ToList();

        var permissionIds = rolePermissions.Select(rp => rp.PermissionId).ToHashSet();

        // 2. Quyền từ nhóm (source_rank = 2)
        var activeGroups = _db.UserGroupMembers
            .Where(gm => gm.UserId == userId && gm.EffectiveFrom <= now && (gm.EffectiveTo == null || gm.EffectiveTo > now))
            .Select(gm => gm.GroupId)
            .ToHashSet();

        var groupPermissions = _db.GroupPermissions
            .Where(gp => activeGroups.Contains(gp.GroupId) && gp.EffectiveFrom <= now && (gp.EffectiveTo == null || gp.EffectiveTo > now))
            .ToList();

        foreach (var gp in groupPermissions)
            permissionIds.Add(gp.PermissionId);

        // 3. Ghi đè cấp người dùng (source_rank = 3)
        var userOverrides = _db.UserPermissionOverrides
            .Where(upo => upo.UserId == userId && upo.EffectiveFrom <= now && (upo.EffectiveTo == null || upo.EffectiveTo > now))
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
                rp.Priority, 1, "role", rp.DepartmentId));
        }

        foreach (var gp in groupPermissions)
        {
            if (!permissions.TryGetValue(gp.PermissionId, out var perm)) continue;
            candidates.Add(new ResolvedPermission(
                gp.PermissionId, perm.PermissionCode, gp.EffectCode,
                gp.Priority, 2, "group", gp.DepartmentId));
        }

        foreach (var upo in userOverrides)
        {
            if (!permissions.TryGetValue(upo.PermissionId, out var perm)) continue;
            candidates.Add(new ResolvedPermission(
                upo.PermissionId, perm.PermissionCode, upo.EffectCode,
                upo.Priority, 3, "user_override", upo.DepartmentId));
        }

        // Giải quyết: nhóm theo PermissionId, chọn bản ghi thắng
        return candidates
            .GroupBy(c => c.PermissionId)
            .Select(PickWinner)
            .ToList();
    }

    /// <summary>
    /// Chọn bản ghi thắng trong nhóm cùng PermissionId:
    /// 1. source_rank cao nhất (user_override > group > role)
    /// 2. priority cao nhất
    /// 3. deny thắng allow khi cùng priority
    /// </summary>
    private static ResolvedPermission PickWinner(IGrouping<Guid, ResolvedPermission> group)
    {
        return group
            .OrderByDescending(c => c.SourceRank)
            .ThenByDescending(c => c.Priority)
            .ThenBy(c => c.EffectCode == "deny" ? 0 : 1)
            .First();
    }

    /// <summary>
    /// Kiểm tra nhanh người dùng có quyền cụ thể hay không.
    /// </summary>
    public bool HasPermission(Guid userId, string permissionCode)
    {
        var resolved = Resolve(userId);
        var match = resolved.FirstOrDefault(r => r.PermissionCode == permissionCode);
        return match is not null && match.EffectCode == "allow";
    }
}
