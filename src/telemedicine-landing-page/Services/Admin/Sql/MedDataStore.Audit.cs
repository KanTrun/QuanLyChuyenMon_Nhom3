using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>Phần nhật ký kiểm toán và yêu cầu thay đổi quyền.</summary>
public sealed partial class MedDataStore
{
    public void AppendAudit(AuditLog log)
    {
        lock (_lock)
        {
            ValidateJson(log.BeforeJson, "before");
            ValidateJson(log.AfterJson, "after");
            ValidateJson(log.MetadataJson, "metadata");
            var seq = Interlocked.Increment(ref _auditSeq);
            _auditLogs.Add(log with { AuditLogSeq = seq });
            RaiseStateChanged();
        }
    }

    public void AddPermissionChangeRequest(PermissionChangeRequest req)
    {
        lock (_lock)
        {
            _permChangeRequests.Add(req);
            RaiseStateChanged();
        }
    }

    public void UpdatePermissionChangeRequest(PermissionChangeRequest updated)
    {
        lock (_lock)
        {
            var idx = _permChangeRequests.FindIndex(r => r.PermissionChangeRequestId == updated.PermissionChangeRequestId);
            if (idx < 0)
                throw MedDomainException.Constraint("FK_permission_change_request", 547, "Yêu cầu thay đổi quyền không tồn tại.");
            _permChangeRequests[idx] = updated;
            RaiseStateChanged();
        }
    }

    public void AddPermissionChangeItem(PermissionChangeItem item)
    {
        lock (_lock)
        {
            ValidateJson(item.ScopeRuleJson, "scope_rule");
            ValidateJson(item.BeforeJson, "before");
            ValidateJson(item.AfterJson, "after");
            _permChangeItems.Add(item);
            RaiseStateChanged();
        }
    }
}
