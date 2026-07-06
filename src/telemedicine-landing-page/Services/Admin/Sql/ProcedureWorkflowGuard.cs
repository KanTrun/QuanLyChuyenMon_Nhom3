namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>
/// Authorization for procedure workflow actions. Assigned writers may edit/sign drafts
/// without global SCR_PROCEDURES:UPDATE. Checker/approver signing accepts feature-level
/// approval permissions (including page-access VIEW on the approval screen).
/// </summary>
public sealed class ProcedureWorkflowGuard
{
    private static readonly string[] WorkflowApprovalPermissions =
    [
        "SCR_PROCEDURES:APPROVE",
        "SCR_PROCEDURES:PUBLISH",
        "SCR_PROCEDURE_APPROVAL:VIEW",
        "SCR_PROCEDURE_APPROVAL:APPROVE",
        "SCR_PROCEDURE_APPROVAL:PUBLISH",
        "SCR_PROCEDURES_WORKSPACE:APPROVE",
        "SCR_PROCEDURES_WORKSPACE:PUBLISH",
    ];

    private readonly AdminActionGuard _guard;
    private readonly ProcedureSignoffService _signoffs;

    public ProcedureWorkflowGuard(AdminActionGuard guard, ProcedureSignoffService signoffs)
    {
        _guard = guard;
        _signoffs = signoffs;
    }

    public bool CanSign(Guid versionId, string role, Guid userId, string? actionLabel = null)
    {
        if (userId != Guid.Empty && _signoffs.CanUserSign(versionId, role, userId, out _))
        {
            if (string.Equals(role, "writer", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (role is "checker" or "approver" && _guard.HasPermission(WorkflowApprovalPermissions))
            {
                return true;
            }
        }

        var permission = role is "checker" or "approver"
            ? "SCR_PROCEDURES:APPROVE"
            : "SCR_PROCEDURES:UPDATE";
        return _guard.CanDo(permission, actionLabel ?? "ký xác nhận quy trình");
    }

    public bool CanPublish(Guid versionId, Guid userId, string? actionLabel = null)
    {
        if (userId != Guid.Empty
            && _signoffs.CanUserSign(versionId, "approver", userId, out _)
            && _guard.HasPermission(WorkflowApprovalPermissions))
        {
            return true;
        }

        return _guard.CanDo("SCR_PROCEDURES:PUBLISH", actionLabel ?? "phê duyệt và ban hành quy trình");
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
