using System.Text.Json;
using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin.Sql;

public sealed record ProcedureRuntimeDecision(bool Allowed, bool WarnOnly, string? Message);

public sealed class ProcedureRuntimeGuard
{
    private static readonly ProcedureRuntimeDecision Allow = new(true, false, null);

    private readonly IMedDataStore _store;
    private readonly ICurrentUserContext _userContext;
    private readonly AuditTrailService _audit;

    public ProcedureRuntimeGuard(
        IMedDataStore store,
        ICurrentUserContext userContext,
        AuditTrailService audit)
    {
        _store = store;
        _userContext = userContext;
        _audit = audit;
    }

    public ProcedureRuntimeDecision EvaluatePermission(string permissionCode)
    {
        if (_userContext.CurrentUser is null || string.IsNullOrWhiteSpace(permissionCode))
        {
            return Allow;
        }

        var parts = permissionCode.Split(':', 2, StringSplitOptions.TrimEntries);
        var screenCode = parts[0];
        var actionCode = parts.Length > 1 ? parts[1].ToLowerInvariant() : null;
        var screen = _store.Screens.FirstOrDefault(s =>
            string.Equals(s.ScreenCode, screenCode, StringComparison.OrdinalIgnoreCase) &&
            s.Status == "active");
        if (screen is null)
        {
            return Allow;
        }

        var now = DateTime.UtcNow;
        var activeVersions = _store.ProcedureVersions
            .Where(v => v.StatusCode == "active" &&
                (v.EffectiveFrom is null || v.EffectiveFrom <= now) &&
                (v.EffectiveTo is null || v.EffectiveTo > now))
            .ToDictionary(v => v.ProcedureVersionId);

        var mapping = _store.ProcedureScreenMappings
            .Where(m => activeVersions.ContainsKey(m.ProcedureVersionId) &&
                m.ScreenId == screen.ScreenId &&
                (string.IsNullOrWhiteSpace(m.ActionCode) ||
                 actionCode is null ||
                 string.Equals(m.ActionCode, actionCode, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(m => string.Equals(m.EnforcementMode, "block", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();
        if (mapping is null)
        {
            return Allow;
        }

        var version = activeVersions[mapping.ProcedureVersionId];
        var firstStep = _store.ProcedureSteps
            .Where(s => s.ProcedureVersionId == version.ProcedureVersionId && s.IsRequired)
            .OrderBy(s => s.StepNo)
            .FirstOrDefault();
        if (firstStep?.ActorRoleId is not Guid actorRoleId ||
            UserHasRoleForStep(actorRoleId, version.DepartmentId))
        {
            return Allow;
        }

        var roleName = _store.Roles.FirstOrDefault(r => r.RoleId == actorRoleId)?.Name ?? "vai trò phụ trách";
        var message = $"Quy trình \"{version.Title}\" yêu cầu bước đầu do {roleName} thực hiện.";
        WriteDeviationAudit(permissionCode, version, mapping, firstStep, message);
        var warnOnly = !string.Equals(mapping.EnforcementMode, "block", StringComparison.OrdinalIgnoreCase);
        return new ProcedureRuntimeDecision(warnOnly, warnOnly, message);
    }

    private bool UserHasRoleForStep(Guid actorRoleId, Guid? departmentId)
    {
        var user = _userContext.CurrentUser;
        if (user is null) return false;
        var now = DateTime.UtcNow;
        return _store.UserRoles.Any(ur =>
            ur.UserId == user.UserId &&
            ur.RoleId == actorRoleId &&
            ur.EffectiveFrom <= now &&
            (ur.EffectiveTo is null || ur.EffectiveTo > now) &&
            (departmentId is null || ur.DepartmentId is null || ur.DepartmentId == departmentId));
    }

    private void WriteDeviationAudit(
        string permissionCode,
        ProcedureVersion version,
        ProcedureScreenMapping mapping,
        ProcedureStep step,
        string message)
    {
        _audit.Append(new AuditLog
        {
            CorrelationId = Guid.NewGuid(),
            ActorUserId = _userContext.CurrentUser?.UserId,
            ActionCode = "update",
            TargetType = "procedure_runtime_guard",
            TargetId = version.ProcedureVersionId.ToString(),
            DepartmentId = version.DepartmentId,
            MetadataJson = JsonSerializer.Serialize(new
            {
                permissionCode,
                mapping.ProcedureScreenMappingId,
                step.ProcedureStepId,
                mapping.EnforcementMode,
                message
            })
        });
    }
}
