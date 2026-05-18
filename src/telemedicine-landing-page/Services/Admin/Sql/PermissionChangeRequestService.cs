using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>Dịch vụ quản lý yêu cầu thay đổi quyền (workflow đầy đủ).</summary>
public sealed class PermissionChangeRequestService
{
    private readonly IMedDataStore _store;
    private readonly AuditTrailService _audit;

    public PermissionChangeRequestService(IMedDataStore store, AuditTrailService audit)
    {
        _store = store;
        _audit = audit;
    }

    /// <summary>Tạo yêu cầu thay đổi quyền mới (trạng thái: bản nháp).</summary>
    public PermissionChangeRequest CreateDraft(
        Guid actorUserId, string targetType,
        Guid? targetRoleId, Guid? targetGroupId, Guid? targetUserId,
        string reason, DateTime effectiveAt)
    {
        int targetCount = (targetRoleId.HasValue ? 1 : 0)
                        + (targetGroupId.HasValue ? 1 : 0)
                        + (targetUserId.HasValue ? 1 : 0);
        if (targetCount != 1)
            throw MedDomainException.Constraint("CK_permission_change_target_one", 50010,
                "Phải chọn đúng một đối tượng mục tiêu (vai trò, nhóm, hoặc người dùng).");

        if (string.IsNullOrWhiteSpace(reason))
            throw MedDomainException.Constraint("CK_permission_change_reason", 50011,
                "Lý do thay đổi không được để trống.");

        var request = new PermissionChangeRequest
        {
            ChangeStatus = "draft",
            TargetType = targetType,
            TargetRoleId = targetRoleId,
            TargetGroupId = targetGroupId,
            TargetUserId = targetUserId,
            Reason = reason,
            RequestedBy = actorUserId,
            EffectiveAt = effectiveAt
        };
        _store.AddPermissionChangeRequest(request);
        return request;
    }

    /// <summary>Gửi yêu cầu để phê duyệt (draft → pending_approval).</summary>
    public void SubmitForApproval(Guid requestId, Guid actorUserId)
    {
        var req = GetRequestOrThrow(requestId);
        if (req.ChangeStatus != "draft")
            throw MedDomainException.Constraint("CK_permission_change_submit", 50012,
                "Chỉ có thể gửi yêu cầu ở trạng thái bản nháp.");

        var items = _store.PermissionChangeItems
            .Where(i => i.PermissionChangeRequestId == requestId).ToList();
        if (items.Count == 0)
            throw MedDomainException.Constraint("CK_permission_change_items_required", 50013,
                "Yêu cầu phải có ít nhất một mục thay đổi.");

        _store.UpdatePermissionChangeRequest(req with { ChangeStatus = "pending_approval" });
        _audit.Append(new AuditLog
        {
            CorrelationId = Guid.NewGuid(),
            ActorUserId = actorUserId,
            ActionCode = "submit",
            TargetType = "permission_change_request",
            TargetId = requestId.ToString()
        });
    }

    /// <summary>Phê duyệt yêu cầu (pending_approval → applied hoặc scheduled).</summary>
    public void Approve(Guid requestId, Guid approverUserId, bool schedule = false)
    {
        var req = GetRequestOrThrow(requestId);
        if (req.ChangeStatus != "pending_approval")
            throw MedDomainException.Constraint("CK_permission_change_approve", 50014,
                "Chỉ có thể phê duyệt yêu cầu đang chờ phê duyệt.");

        var newStatus = schedule ? "scheduled" : "applied";
        _store.UpdatePermissionChangeRequest(req with
        {
            ChangeStatus = newStatus,
            ApprovedBy = approverUserId,
            ApprovedAt = DateTime.UtcNow,
            AppliedAt = schedule ? null : DateTime.UtcNow,
            AppliedBy = schedule ? null : approverUserId
        });
        _audit.Append(new AuditLog
        {
            CorrelationId = Guid.NewGuid(),
            ActorUserId = approverUserId,
            ActionCode = "approve",
            TargetType = "permission_change_request",
            TargetId = requestId.ToString()
        });
    }

    /// <summary>Từ chối yêu cầu (pending_approval → rejected).</summary>
    public void Reject(Guid requestId, Guid approverUserId, string reason)
    {
        var req = GetRequestOrThrow(requestId);
        if (req.ChangeStatus != "pending_approval")
            throw MedDomainException.Constraint("CK_permission_change_reject", 50015,
                "Chỉ có thể từ chối yêu cầu đang chờ phê duyệt.");

        _store.UpdatePermissionChangeRequest(req with { ChangeStatus = "rejected" });
        _audit.Append(new AuditLog
        {
            CorrelationId = Guid.NewGuid(),
            ActorUserId = approverUserId,
            ActionCode = "reject",
            TargetType = "permission_change_request",
            TargetId = requestId.ToString(),
            MetadataJson = $"{{\"reason\":\"{reason}\"}}"
        });
    }

    /// <summary>Hủy yêu cầu (draft hoặc scheduled → cancelled).</summary>
    public void Cancel(Guid requestId, Guid actorUserId)
    {
        var req = GetRequestOrThrow(requestId);
        if (req.ChangeStatus != "draft" && req.ChangeStatus != "scheduled")
            throw MedDomainException.Constraint("CK_permission_change_cancel", 50016,
                "Chỉ có thể hủy yêu cầu ở trạng thái bản nháp hoặc đã lên lịch.");

        _store.UpdatePermissionChangeRequest(req with { ChangeStatus = "cancelled" });
        _audit.Append(new AuditLog
        {
            CorrelationId = Guid.NewGuid(),
            ActorUserId = actorUserId,
            ActionCode = "revoke",
            TargetType = "permission_change_request",
            TargetId = requestId.ToString()
        });
    }

    /// <summary>Thêm mục thay đổi vào yêu cầu (chỉ khi trạng thái draft).</summary>
    public void AddItem(Guid requestId, PermissionChangeItem item)
    {
        var req = GetRequestOrThrow(requestId);
        if (req.ChangeStatus != "draft")
            throw MedDomainException.Constraint("CK_permission_change_items_draft", 50017,
                "Chỉ có thể thêm mục khi yêu cầu ở trạng thái bản nháp.");

        _store.AddPermissionChangeItem(item with { PermissionChangeRequestId = requestId });
    }

    /// <summary>Lấy tất cả yêu cầu thay đổi quyền.</summary>
    public IReadOnlyList<PermissionChangeRequest> GetAll() => _store.PermissionChangeRequests;

    /// <summary>Lấy yêu cầu theo trạng thái.</summary>
    public IReadOnlyList<PermissionChangeRequest> GetByStatus(string status)
        => _store.PermissionChangeRequests.Where(r => r.ChangeStatus == status).ToList();

    private PermissionChangeRequest GetRequestOrThrow(Guid requestId)
    {
        return _store.PermissionChangeRequests
                   .FirstOrDefault(r => r.PermissionChangeRequestId == requestId)
               ?? throw MedDomainException.Constraint("FK_permission_change_request", 547,
                   "Yêu cầu thay đổi quyền không tồn tại.");
    }
}
