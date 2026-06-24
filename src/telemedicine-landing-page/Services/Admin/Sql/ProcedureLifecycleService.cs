using TelemedicineLandingPage.Data;
using TelemedicineLandingPage.Application.Workflow;
using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin;

namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>
/// Dịch vụ vòng đời phiên bản quy trình chuyên môn.
/// Quản lý chuyển trạng thái: draft → pending_approval → active (chỉ 1 bản đang hiệu lực).
/// </summary>
public sealed class ProcedureLifecycleService
{
    private readonly MedDbContext _db;
    private readonly AuditTrailService _audit;
    private readonly IWorkflowGuard<ProcedureVersion, string> _workflow;
    private readonly ProcedureDocumentSnapshotService? _documents;

    public ProcedureLifecycleService(MedDbContext db, AuditTrailService audit)
        : this(db, audit, new ProcedureVersionWorkflowGuard(audit))
    {
    }

    public ProcedureLifecycleService(
        MedDbContext db,
        AuditTrailService audit,
        IWorkflowGuard<ProcedureVersion, string> workflow)
    {
        _db = db;
        _audit = audit;
        _workflow = workflow;
    }

    public ProcedureLifecycleService(
        MedDbContext db,
        AuditTrailService audit,
        IWorkflowGuard<ProcedureVersion, string> workflow,
        ProcedureDocumentSnapshotService documents)
    {
        _db = db;
        _audit = audit;
        _workflow = workflow;
        _documents = documents;
    }

    /// <summary>Tạo phiên bản mới cho quy trình (trạng thái draft).</summary>
    public ProcedureVersion CreateDraft(Guid procedureId, string title, Guid createdBy)
    {
        var proc = _db.Procedures.FirstOrDefault(p => p.ProcedureId == procedureId)
            ?? throw MedDomainException.Constraint("FK_procedure", 547,
                "Quy trình không tồn tại.");

        var existingVersions = _db.ProcedureVersions
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

        _db.ProcedureVersions.Add(version);
        _db.SaveChanges();
        return version;
    }

    /// <summary>Gửi phiên bản để phê duyệt (draft → pending_approval).</summary>
    public void Submit(Guid versionId, Guid submittedBy)
    {
        var ver = GetVersionOrThrow(versionId);
        EnsureTransition(ver, "pending_approval", "CK_procedure_version_submit", 50020,
            "Chỉ có thể gửi phiên bản đang ở trạng thái draft.");
        if (ver.StatusCode != "draft")
            throw MedDomainException.Constraint("CK_procedure_version_submit", 50020,
                "Chỉ có thể gửi phiên bản ở trạng thái bản nháp.");

        var steps = _db.ProcedureSteps
            .Where(s => s.ProcedureVersionId == versionId).ToList();
        if (steps.Count == 0)
            throw MedDomainException.Constraint("CK_procedure_version_steps_required", 50021,
                "Phiên bản phải có ít nhất một bước quy trình.");

        EnsureDocumentReady(versionId, requireAllSignoffs: false);
        EnsureCurrentSignoff(versionId, "writer", "CK_procedure_writer_signoff_required", 50027);

        var updated = ver with
        {
            StatusCode = "pending_approval",
            SubmittedBy = submittedBy,
            SubmittedAt = DateTime.UtcNow
        };
        _db.ProcedureVersions.Entry(ver).CurrentValues.SetValues(updated);
        _db.SaveChanges();
        _workflow.OnTransitioned(updated, ver.StatusCode, updated.StatusCode, submittedBy);

        _audit.Append(new AuditLog
        {
            CorrelationId = Guid.NewGuid(),
            ActorUserId = submittedBy,
            ActionCode = "submit",
            TargetType = "procedure_version",
            TargetId = versionId.ToString()
        });
    }

    /// <summary>Phê duyệt và xuất bản phiên bản (pending_approval → active). Hủy bản active cũ.</summary>
    public void Publish(Guid versionId, Guid approvedBy)
    {
        var ver = GetVersionOrThrow(versionId);
        EnsureTransition(ver, "active", "CK_procedure_version_publish", 50022,
            "Chỉ có thể xuất bản phiên bản đang chờ phê duyệt.");
        if (ver.StatusCode != "pending_approval")
            throw MedDomainException.Constraint("CK_procedure_version_publish", 50022,
                "Chỉ có thể xuất bản phiên bản đang chờ phê duyệt.");

        // Hủy kích hoạt phiên bản đang hiệu lực hiện tại (one-active guard)
        EnsureDocumentReady(versionId, requireAllSignoffs: true);
        EnsurePublishSeparation(versionId, approvedBy);

        var currentPublished = _db.ProcedureVersions
            .Where(v => v.ProcedureId == ver.ProcedureId && v.StatusCode == "active")
            .ToList();

        foreach (var old in currentPublished)
        {
            var superseded = old with
            {
                StatusCode = "superseded",
                EffectiveTo = DateTime.UtcNow
            };
            _db.ProcedureVersions.Entry(old).CurrentValues.SetValues(superseded);
        }

        var published = ver with
        {
            StatusCode = "active",
            ApprovedBy = approvedBy,
            ApprovedAt = DateTime.UtcNow,
            PublishedBy = approvedBy,
            PublishedAt = DateTime.UtcNow,
            EffectiveFrom = DateTime.UtcNow
        };
        _db.ProcedureVersions.Entry(ver).CurrentValues.SetValues(published);
        _db.SaveChanges();

        foreach (var old in currentPublished)
        {
            _workflow.OnTransitioned(old, "active", "superseded", approvedBy, $"Superseded by {versionId}");
        }
        _workflow.OnTransitioned(published, ver.StatusCode, published.StatusCode, approvedBy);

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
        EnsureTransition(ver, "rejected", "CK_procedure_version_reject", 50023,
            "Chỉ có thể từ chối phiên bản đang chờ phê duyệt.");
        if (ver.StatusCode != "pending_approval")
            throw MedDomainException.Constraint("CK_procedure_version_reject", 50023,
                "Chỉ có thể từ chối phiên bản đang chờ phê duyệt.");

        var rejected = ver with
        {
            StatusCode = "rejected",
            ChangeReason = reason
        };
        _db.ProcedureVersions.Entry(ver).CurrentValues.SetValues(rejected);
        _db.SaveChanges();
        _workflow.OnTransitioned(rejected, ver.StatusCode, rejected.StatusCode, rejectedBy, reason);

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

    /// <summary>Thu hồi phiên bản đã xuất bản (active → archived).</summary>
    public void Withdraw(Guid versionId, Guid withdrawnBy, string reason)
    {
        var ver = GetVersionOrThrow(versionId);
        EnsureTransition(ver, "archived", "CK_procedure_version_withdraw", 50024,
            "Chỉ có thể thu hồi phiên bản active.");
        if (ver.StatusCode != "active")
            throw MedDomainException.Constraint("CK_procedure_version_withdraw", 50024,
                "Chỉ có thể thu hồi phiên bản đã xuất bản.");

        var withdrawn = ver with
        {
            StatusCode = "archived",
            EffectiveTo = DateTime.UtcNow,
            ChangeReason = reason
        };
        _db.ProcedureVersions.Entry(ver).CurrentValues.SetValues(withdrawn);
        _db.SaveChanges();
        _workflow.OnTransitioned(withdrawn, ver.StatusCode, withdrawn.StatusCode, withdrawnBy, reason);

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
    public void Archive(Guid versionId, Guid archivedBy, string? reason = null)
    {
        var ver = GetVersionOrThrow(versionId);
        EnsureTransition(ver, "archived", "CK_procedure_version_archive", 50025,
            "Không thể lưu trữ phiên bản từ trạng thái hiện tại.");

        var archived = ver with
        {
            StatusCode = "archived",
            EffectiveTo = DateTime.UtcNow,
            ChangeReason = reason ?? ver.ChangeReason
        };
        _db.ProcedureVersions.Entry(ver).CurrentValues.SetValues(archived);
        _db.SaveChanges();
        _workflow.OnTransitioned(archived, ver.StatusCode, archived.StatusCode, archivedBy, reason);

        _audit.Append(new AuditLog
        {
            CorrelationId = Guid.NewGuid(),
            ActorUserId = archivedBy,
            ActionCode = "archive",
            TargetType = "procedure_version",
            TargetId = versionId.ToString()
        });
    }

    public void RestoreDraft(Guid versionId, Guid restoredBy, string? reason = null)
    {
        var ver = GetVersionOrThrow(versionId);
        EnsureTransition(ver, "draft", "CK_procedure_version_restore", 50026,
            "Chi co the khoi phuc phien ban archived hoac rejected ve draft.");

        var restored = ver with
        {
            StatusCode = "draft",
            EffectiveTo = null,
            ChangeReason = reason ?? ver.ChangeReason
        };
        _db.ProcedureVersions.Entry(ver).CurrentValues.SetValues(restored);
        _db.SaveChanges();
        _workflow.OnTransitioned(restored, ver.StatusCode, restored.StatusCode, restoredBy, reason);

        _audit.Append(new AuditLog
        {
            CorrelationId = Guid.NewGuid(),
            ActorUserId = restoredBy,
            ActionCode = "restore",
            TargetType = "procedure_version",
            TargetId = versionId.ToString()
        });
    }

    public ProcedureVersion? GetActiveVersion(Guid procedureId)
    {
        return _db.ProcedureVersions
            .FirstOrDefault(v => v.ProcedureId == procedureId && v.StatusCode == "active");
    }

    /// <summary>Lấy tất cả phiên bản của quy trình.</summary>
    public IReadOnlyList<ProcedureVersion> GetVersions(Guid procedureId)
    {
        return _db.ProcedureVersions
            .Where(v => v.ProcedureId == procedureId)
            .OrderByDescending(v => v.VersionNo)
            .ToList();
    }

    private ProcedureVersion GetVersionOrThrow(Guid versionId)
    {
        return _db.ProcedureVersions
                   .FirstOrDefault(v => v.ProcedureVersionId == versionId)
               ?? throw MedDomainException.Constraint("FK_procedure_version", 547,
                   "Phiên bản quy trình không tồn tại.");
    }

    private void EnsureTransition(
        ProcedureVersion version,
        string targetState,
        string constraintName,
        int errorNumber,
        string message)
    {
        if (!_workflow.CanTransition(version.StatusCode, targetState))
        {
            throw MedDomainException.Constraint(constraintName, errorNumber, message);
        }
    }

    private void EnsureDocumentReady(Guid versionId, bool requireAllSignoffs)
    {
        if (_documents is null) return;
        var readiness = _documents.CheckReadiness(versionId, requireAllSignoffs);
        if (!readiness.IsReady)
        {
            throw MedDomainException.Constraint(
                "CK_procedure_document_ready",
                50028,
                "Quy trình chưa đủ điều kiện ban hành: " + string.Join(", ", readiness.MissingItems));
        }
    }

    private void EnsureCurrentSignoff(Guid versionId, string role, string constraintName, int errorNumber)
    {
        if (_documents is null) return;
        var snapshot = _documents.GetSnapshot(versionId);
        if (!_documents.HasCurrentSignoff(snapshot, role))
        {
            throw MedDomainException.Constraint(
                constraintName,
                errorNumber,
                $"Thiếu chữ ký {SignoffRoleLabel(role)} hợp lệ hoặc chữ ký đã cũ sau khi nội dung thay đổi.");
        }
    }

    private void EnsurePublishSeparation(Guid versionId, Guid approvedBy)
    {
        if (_documents is null) return;
        var snapshot = _documents.GetSnapshot(versionId);
        var hash = _documents.ComputeContentHash(versionId);
        var writerUserId = snapshot.Signoffs
            .Where(signoff => string.Equals(signoff.SignoffRole, "writer", StringComparison.OrdinalIgnoreCase)
                && string.Equals(signoff.ContentHashSha256, hash, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(signoff => signoff.SignedAt)
            .FirstOrDefault()?.SignerUserId;
        var checkerUserId = snapshot.Signoffs
            .Where(signoff => string.Equals(signoff.SignoffRole, "checker", StringComparison.OrdinalIgnoreCase)
                && string.Equals(signoff.ContentHashSha256, hash, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(signoff => signoff.SignedAt)
            .FirstOrDefault()?.SignerUserId;

        if (writerUserId == approvedBy || checkerUserId == approvedBy)
        {
            throw MedDomainException.Constraint(
                "CK_procedure_publish_separation",
                50029,
                "Người ban hành phải khác người viết và người kiểm tra.");
        }
    }

    private static string SignoffRoleLabel(string role) => role switch
    {
        "writer" => "Người viết",
        "checker" => "Người kiểm tra",
        "approver" => "Người phê duyệt",
        _ => role
    };
}
