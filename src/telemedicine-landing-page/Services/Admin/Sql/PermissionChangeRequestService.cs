using TelemedicineLandingPage.Data;
using TelemedicineLandingPage.Models.Admin.Sql;
using Microsoft.EntityFrameworkCore;

namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>Dịch vụ quản lý yêu cầu thay đổi quyền (workflow đầy đủ).</summary>
public sealed class PermissionChangeRequestService
{
    private readonly MedDbContext _db;
    private readonly AuditTrailService _audit;

    public PermissionChangeRequestService(MedDbContext db, AuditTrailService audit)
    {
        _db = db;
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
        _db.PermissionChangeRequests.Add(request);
        _db.SaveChanges();
        return request;
    }

    /// <summary>Gửi yêu cầu để phê duyệt (draft → pending_approval).</summary>
    public void SubmitForApproval(Guid requestId, Guid actorUserId)
    {
        var req = GetRequestOrThrow(requestId);
        if (req.ChangeStatus != "draft")
            throw MedDomainException.Constraint("CK_permission_change_submit", 50012,
                "Chỉ có thể gửi yêu cầu ở trạng thái bản nháp.");

        var items = _db.PermissionChangeItems
            .Where(i => i.PermissionChangeRequestId == requestId).ToList();
        if (items.Count == 0)
            throw MedDomainException.Constraint("CK_permission_change_items_required", 50013,
                "Yêu cầu phải có ít nhất một mục thay đổi.");

        var updated = req with { ChangeStatus = "pending_approval" };
        _db.PermissionChangeRequests.Entry(req).CurrentValues.SetValues(updated);
        _db.SaveChanges();

        NotifyApprovers(requestId, req.RequestedBy);

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

        var effectiveAt = schedule
            ? req.EffectiveAt
            : (req.EffectiveAt > DateTime.UtcNow ? DateTime.UtcNow : req.EffectiveAt);
        var updated = req with
        {
            ChangeStatus = "scheduled",
            ApprovedBy = approverUserId,
            ApprovedAt = DateTime.UtcNow,
            AppliedAt = null,
            AppliedBy = approverUserId,
            EffectiveAt = effectiveAt,
            ErrorMessage = null
        };
        _db.PermissionChangeRequests.Entry(req).CurrentValues.SetValues(updated);
        _db.SaveChanges();

        if (!schedule)
        {
            _db.Database.ExecuteSqlRaw("EXEC med.sp_apply_due_permission_changes");

            var applied = GetRequestOrThrow(requestId);
            if (applied.ChangeStatus == "failed")
            {
                throw MedDomainException.Constraint("CK_permission_change_apply_failed", 50018,
                    applied.ErrorMessage ?? "Không thể áp dụng quyền sau khi phê duyệt.");
            }

            if (applied.ChangeStatus != "applied")
            {
                throw MedDomainException.Constraint("CK_permission_change_apply_pending", 50019,
                    "Yêu cầu đã được duyệt nhưng chưa được áp dụng vào quyền truy cập.");
            }
        }

        NotifyRequester(requestId, "permission_change", schedule
            ? "Yêu cầu quyền đã được phê duyệt và lên lịch áp dụng."
            : "Yêu cầu quyền đã được phê duyệt và áp dụng.");

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

        var updated = req with { ChangeStatus = "rejected" };
        _db.PermissionChangeRequests.Entry(req).CurrentValues.SetValues(updated);
        _db.SaveChanges();

        NotifyRequester(requestId, "permission_change", $"Yêu cầu quyền bị từ chối: {reason}");

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

        var updated = req with { ChangeStatus = "cancelled" };
        _db.PermissionChangeRequests.Entry(req).CurrentValues.SetValues(updated);
        _db.SaveChanges();

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

        _db.PermissionChangeItems.Add(item with { PermissionChangeRequestId = requestId });
        _db.SaveChanges();
    }

    /// <summary>Lấy tất cả yêu cầu thay đổi quyền.</summary>
    public IReadOnlyList<PermissionChangeRequest> GetAll()
        => _db.PermissionChangeRequests.OrderByDescending(r => r.RequestedAt).ToList();

    /// <summary>Lấy yêu cầu theo trạng thái.</summary>
    public IReadOnlyList<PermissionChangeRequest> GetByStatus(string status)
        => _db.PermissionChangeRequests.Where(r => r.ChangeStatus == status).ToList();

    private PermissionChangeRequest GetRequestOrThrow(Guid requestId)
    {
        return _db.PermissionChangeRequests
                   .FirstOrDefault(r => r.PermissionChangeRequestId == requestId)
               ?? throw MedDomainException.Constraint("FK_permission_change_request", 547,
                   "Yêu cầu thay đổi quyền không tồn tại.");
    }

    private void NotifyApprovers(Guid requestId, Guid requestedByUserId)
    {
        var approverRoleIds = _db.Roles
            .Where(r => r.Status == "active" && r.Code == "SYSTEM_ADMIN")
            .Select(r => r.RoleId)
            .ToHashSet();

        var approverUserIds = _db.UserRoles
            .Where(ur => approverRoleIds.Contains(ur.RoleId) && ur.EffectiveTo == null)
            .Select(ur => ur.UserId)
            .ToHashSet();

        foreach (var adminUserId in _db.Users
                     .Where(u => u.Status == "active" && u.Username == "admin")
                     .Select(u => u.UserId))
        {
            approverUserIds.Add(adminUserId);
        }

        var requesterName = _db.Users
            .Where(u => u.UserId == requestedByUserId)
            .Select(u => u.FullName)
            .FirstOrDefault() ?? "Người dùng";

        foreach (var approverUserId in approverUserIds)
        {
            _db.Notifications.Add(new MedNotification
            {
                RecipientUserId = approverUserId,
                NotificationType = "in_app",
                Title = "Có yêu cầu quyền truy cập mới",
                Body = $"{requesterName} vừa gửi yêu cầu cấp quyền truy cập cần bạn phê duyệt.",
                Severity = "info",
                SourceType = "permission_change",
                SourceId = requestId.ToString()
            });
        }

        _db.SaveChanges();
    }

    private void NotifyRequester(Guid requestId, string sourceType, string body)
    {
        var req = GetRequestOrThrow(requestId);

        _db.Notifications.Add(new MedNotification
        {
            RecipientUserId = req.RequestedBy,
            NotificationType = "in_app",
            Title = "Cập nhật yêu cầu quyền truy cập",
            Body = body,
            Severity = req.ChangeStatus == "rejected" ? "warning" : "info",
            SourceType = sourceType,
            SourceId = requestId.ToString()
        });
        _db.SaveChanges();
    }
}
