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
    private readonly IMedDataStore _store;

    public EffectivePermissionResolver(IMedDataStore store)
    {
        _store = store;
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
        var activeRoles = _store.UserRoles
            .Where(ur => ur.UserId == userId && IsEffective(ur.EffectiveFrom, ur.EffectiveTo, now))
            .Select(ur => ur.RoleId)
            .ToHashSet();

        foreach (var rp in _store.RolePermissions)
        {
            if (!activeRoles.Contains(rp.RoleId)) continue;
            if (!IsEffective(rp.EffectiveFrom, rp.EffectiveTo, now)) continue;
            var perm = _store.Permissions.FirstOrDefault(p => p.PermissionId == rp.PermissionId);
            if (perm is null || perm.Status != "active") continue;

            candidates.Add(new ResolvedPermission(
                rp.PermissionId, perm.PermissionCode, rp.EffectCode,
                rp.Priority, 1, "role", rp.DepartmentId));
        }

        // 2. Quyền từ nhóm (source_rank = 2)
        var activeGroups = _store.UserGroupMembers
            .Where(gm => gm.UserId == userId && IsEffective(gm.EffectiveFrom, gm.EffectiveTo, now))
            .Select(gm => gm.GroupId)
            .ToHashSet();

        foreach (var gp in _store.GroupPermissions)
        {
            if (!activeGroups.Contains(gp.GroupId)) continue;
            if (!IsEffective(gp.EffectiveFrom, gp.EffectiveTo, now)) continue;
            var perm = _store.Permissions.FirstOrDefault(p => p.PermissionId == gp.PermissionId);
            if (perm is null || perm.Status != "active") continue;

            candidates.Add(new ResolvedPermission(
                gp.PermissionId, perm.PermissionCode, gp.EffectCode,
                gp.Priority, 2, "group", gp.DepartmentId));
        }

        // 3. Ghi đè cấp người dùng (source_rank = 3)
        foreach (var upo in _store.UserPermissionOverrides)
        {
            if (upo.UserId != userId) continue;
            if (!IsEffective(upo.EffectiveFrom, upo.EffectiveTo, now)) continue;
            var perm = _store.Permissions.FirstOrDefault(p => p.PermissionId == upo.PermissionId);
            if (perm is null || perm.Status != "active") continue;

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

    private static bool IsEffective(DateTime from, DateTime? to, DateTime now)
        => from <= now && (to is null || to.Value > now);

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
