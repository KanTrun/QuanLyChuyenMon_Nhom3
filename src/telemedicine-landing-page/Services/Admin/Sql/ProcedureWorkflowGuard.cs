namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>
/// Authorization for procedure workflow actions. Assigned writers may edit/sign drafts
/// without global SCR_PROCEDURES:UPDATE; RBAC still applies for other roles and actions.
/// </summary>
public sealed class ProcedureWorkflowGuard
{
    private readonly AdminActionGuard _guard;
    private readonly ProcedureSignoffService _signoffs;

    public ProcedureWorkflowGuard(AdminActionGuard guard, ProcedureSignoffService signoffs)
    {
        _guard = guard;
        _signoffs = signoffs;
    }

    public bool CanSign(Guid versionId, string role, Guid userId, string? actionLabel = null)
    {
        if (userId != Guid.Empty
            && string.Equals(role, "writer", StringComparison.OrdinalIgnoreCase)
            && _signoffs.CanUserSign(versionId, role, userId, out _))
        {
            return true;
        }

        var permission = role is "checker" or "approver"
            ? "SCR_PROCEDURES:APPROVE"
            : "SCR_PROCEDURES:UPDATE";
        return _guard.CanDo(permission, actionLabel ?? "ký xác nhận quy trình");
    }

    public bool CanEditDraft(Guid versionId, Guid userId)
    {
        if (userId != Guid.Empty && _signoffs.CanUserEditDraft(versionId, userId, out _))
        {
            return true;
        }

        return _guard.CanDo("SCR_PROCEDURES:UPDATE", "chỉnh sửa bản nháp");
    }

    public bool CanCreateOrUpdate(bool isUpdate, Guid? versionId, Guid userId, string actionLabel)
    {
        if (isUpdate && versionId.HasValue && userId != Guid.Empty
            && _signoffs.CanUserEditDraft(versionId.Value, userId, out _))
        {
            return true;
        }

        var permission = isUpdate ? "SCR_PROCEDURES:UPDATE" : "SCR_PROCEDURES:CREATE";
        return _guard.CanDo(permission, actionLabel);
    }
}
