using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin.Sql;

public sealed partial class PermissionChangeRequestService
{
    private void AddRequestNotification(PermissionChangeRequest req, string title, string body, string severity)
    {
        var recipients = new[] { req.RequestedBy, req.TargetUserId ?? Guid.Empty }
            .Where(id => id != Guid.Empty)
            .Distinct()
            .Where(id => _db.Users.Any(u => u.UserId == id));
        foreach (var recipient in recipients)
        {
            _db.Notifications.Add(new MedNotification
            {
                RecipientUserId = recipient,
                NotificationType = "permission_change",
                Title = title,
                Body = body,
                Severity = severity,
                SourceType = "permission_change",
                SourceId = req.PermissionChangeRequestId.ToString()
            });
        }
    }

    private void Expire(RolePermission? permission, DateTime now)
    {
        if (permission is not null)
            _db.RolePermissions.Entry(permission).CurrentValues.SetValues(permission with { EffectiveTo = now });
    }

    private void Expire(GroupPermission? permission, DateTime now)
    {
        if (permission is not null)
            _db.GroupPermissions.Entry(permission).CurrentValues.SetValues(permission with { EffectiveTo = now });
    }

    private void Expire(UserPermissionOverride? permission, DateTime now)
    {
        if (permission is not null)
            _db.UserPermissionOverrides.Entry(permission).CurrentValues.SetValues(permission with { EffectiveTo = now });
    }

    private static string NormalizeScope(string? scope)
        => string.Equals(scope, "all", StringComparison.OrdinalIgnoreCase) ? "global" : scope ?? "global";

    private static MedDomainException MissingTarget(string target)
        => MedDomainException.Constraint("CK_permission_change_target", 50019,
            $"Yêu cầu thiếu {target} cần áp dụng.");
}
