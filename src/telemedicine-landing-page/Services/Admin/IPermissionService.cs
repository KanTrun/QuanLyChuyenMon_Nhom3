using TelemedicineLandingPage.Models.Admin;

namespace TelemedicineLandingPage.Services.Admin;

/// <summary>
/// In-memory permission management service: roles, user assignments and an
/// append-only change log used by the Lịch sử thay đổi tab.
/// </summary>
public interface IPermissionService
{
    IReadOnlyList<RoleRecord> ListRoles();
    RoleRecord? GetRole(Guid id);
    RoleRecord CreateRole(RoleRecord record);
    RoleRecord UpdateRole(Guid id, RoleRecord updated);
    void DeleteRole(Guid id);
    void UpdateRolePermissions(
        Guid roleId,
        IReadOnlyList<PermissionGrant> grants,
        string reason,
        DateTime effectiveAt,
        string changedBy);

    IReadOnlyList<UserAccountRecord> ListUsers();
    void AssignUserRoles(Guid userId, IReadOnlyList<Guid> roleIds, string reason, string changedBy);

    IReadOnlyList<PermissionChangeLog> GetChangeLog(Guid? targetId = null);

    /// <summary>The list of admin module ids this hospital cares about.</summary>
    IReadOnlyList<string> AdminModules { get; }

    event Action? StateChanged;
}
