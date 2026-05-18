using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>
/// Dịch vụ vòng đời phiên bản quy trình chuyên môn.
/// Quản lý chuyển trạng thái: draft → pending_approval → published (chỉ 1 bản active).
/// </summary>
public sealed class ProcedureLifecycleService
{
    private readonly IMedDataStore _store;
    private readonly AuditTrailService _audit;

    public ProcedureLifecycleService(IMedDataStore store, AuditTrailService audit)
    {
        _store = store;
        _audit = audit;
    }

    /// <summary>Tạo phiên bản mới cho quy trình (trạng thái draft).</summary>
    public ProcedureVersion CreateDraft(Guid procedureId, string title, Guid createdBy)
    {
        var proc = _store.Procedures.FirstOrDefault(p => p.ProcedureId == procedureId)
            ?? throw MedDomainException.Constraint("FK_procedure", 547,
                "Quy trình không tồn tại.");

        var existingVersions = _store.ProcedureVersions
            .Where(v => v.ProcedureId == procedureId)
            .ToList();

        var nextVersionNo = existingVersions.Count > 0
            ? existingVersions.Max(v => v.VersionNo) + 1
            : 1;

        var version = new ProcedureVersion
        {
            ProcedureId = procedureId,
            VersionNo = nextVersionNo,
            VersionLabel = $"v{nextVersionNo}.0",
            StatusCode = "draft",
            DepartmentId = proc.OwnerDepartmentId,
            Title = title,
            CreatedBy = createdBy
        };

        _store.AddProcedureVersion(version);
        return version;
    }

    /// <summary>Gửi phiên bản để phê duyệt (draft → pending_approval).</summary>
    public void Submit(Guid versionId, Guid submittedBy)
    {
        var ver = GetVersionOrThrow(versionId);
        if (ver.StatusCode != "draft")
            throw MedDomainException.Constraint("CK_procedure_version_submit", 50020,
                "Chỉ có thể gửi phiên bản ở trạng thái bản nháp.");

        var steps = _store.ProcedureSteps
            .Where(s => s.ProcedureVersionId == versionId).ToList();
        if (steps.Count == 0)
            throw MedDomainException.Constraint("CK_procedure_version_steps_required", 50021,
                "Phiên bản phải có ít nhất một bước quy trình.");

        _store.UpdateProcedureVersion(ver with
        {
            StatusCode = "pending_approval",
            SubmittedBy = submittedBy,
            SubmittedAt = DateTime.UtcNow
        });

        _audit.Append(new AuditLog
        {
            CorrelationId = Guid.NewGuid(),
            ActorUserId = submittedBy,
            ActionCode = "submit",
            TargetType = "procedure_version",
            TargetId = versionId.ToString()
        });
    }

    /// <summary>Phê duyệt và xuất bản phiên bản (pending_approval → published). Hủy bản published cũ.</summary>
    public void Publish(Guid versionId, Guid approvedBy)
    {
        var ver = GetVersionOrThrow(versionId);
        if (ver.StatusCode != "pending_approval")
            throw MedDomainException.Constraint("CK_procedure_version_publish", 50022,
                "Chỉ có thể xuất bản phiên bản đang chờ phê duyệt.");

        // Hủy kích hoạt phiên bản published hiện tại (one-active guard)
        var currentPublished = _store.ProcedureVersions
            .Where(v => v.ProcedureId == ver.ProcedureId && v.StatusCode == "published")
            .ToList();

        foreach (var old in currentPublished)
        {
            _store.UpdateProcedureVersion(old with
            {
                StatusCode = "superseded",
                EffectiveTo = DateTime.UtcNow
            });
        }

        _store.UpdateProcedureVersion(ver with
        {
            StatusCode = "published",
            ApprovedBy = approvedBy,
            ApprovedAt = DateTime.UtcNow,
            PublishedBy = approvedBy,
            PublishedAt = DateTime.UtcNow,
            EffectiveFrom = DateTime.UtcNow
        });

        _audit.Append(new AuditLog
        {
            CorrelationId = Guid.NewGuid(),
            ActorUserId = approvedBy,
            ActionCode = "publish",
            TargetType = "procedure_version",
            TargetId = versionId.ToString()
        });
    }

    /// <summary>Từ chối phiên bản (pending_approval → rejected).</summary>
    public void Reject(Guid versionId, Guid rejectedBy, string reason)
    {
        var ver = GetVersionOrThrow(versionId);
        if (ver.StatusCode != "pending_approval")
            throw MedDomainException.Constraint("CK_procedure_version_reject", 50023,
                "Chỉ có thể từ chối phiên bản đang chờ phê duyệt.");

        _store.UpdateProcedureVersion(ver with
        {
            StatusCode = "rejected",
            ChangeReason = reason
        });

        _audit.Append(new AuditLog
        {
            CorrelationId = Guid.NewGuid(),
            ActorUserId = rejectedBy,
            ActionCode = "reject",
            TargetType = "procedure_version",
            TargetId = versionId.ToString(),
            MetadataJson = $"{{\"reason\":\"{reason}\"}}"
        });
    }

    /// <summary>Thu hồi phiên bản đã xuất bản (published → withdrawn).</summary>
    public void Withdraw(Guid versionId, Guid withdrawnBy, string reason)
    {
        var ver = GetVersionOrThrow(versionId);
        if (ver.StatusCode != "published")
            throw MedDomainException.Constraint("CK_procedure_version_withdraw", 50024,
                "Chỉ có thể thu hồi phiên bản đã xuất bản.");

        _store.UpdateProcedureVersion(ver with
        {
            StatusCode = "withdrawn",
            EffectiveTo = DateTime.UtcNow,
            ChangeReason = reason
        });

        _audit.Append(new AuditLog
        {
            CorrelationId = Guid.NewGuid(),
            ActorUserId = withdrawnBy,
            ActionCode = "revoke",
            TargetType = "procedure_version",
            TargetId = versionId.ToString()
        });
    }

    /// <summary>Lấy phiên bản đang hoạt động của quy trình.</summary>
    public ProcedureVersion? GetActiveVersion(Guid procedureId)
    {
        return _store.ProcedureVersions
            .FirstOrDefault(v => v.ProcedureId == procedureId && v.StatusCode == "published");
    }

    /// <summary>Lấy tất cả phiên bản của quy trình.</summary>
    public IReadOnlyList<ProcedureVersion> GetVersions(Guid procedureId)
    {
        return _store.ProcedureVersions
            .Where(v => v.ProcedureId == procedureId)
            .OrderByDescending(v => v.VersionNo)
            .ToList();
    }

    private ProcedureVersion GetVersionOrThrow(Guid versionId)
    {
        return _store.ProcedureVersions
                   .FirstOrDefault(v => v.ProcedureVersionId == versionId)
               ?? throw MedDomainException.Constraint("FK_procedure_version", 547,
                   "Phiên bản quy trình không tồn tại.");
    }
}
