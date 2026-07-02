using Microsoft.EntityFrameworkCore;
using TelemedicineLandingPage.Data;
using TelemedicineLandingPage.Application.Workflow;
using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin;
using System.Text.Json;

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
    private readonly IMedDataChangeBus? _changeBus;

    public ProcedureLifecycleService(MedDbContext db, AuditTrailService audit)
        : this(db, audit, new ProcedureVersionWorkflowGuard(audit))
    {
    }

    public ProcedureLifecycleService(
        MedDbContext db,
        AuditTrailService audit,
        IWorkflowGuard<ProcedureVersion, string> workflow)
        : this(db, audit, workflow, documents: null, changeBus: null)
    {
    }

    public ProcedureLifecycleService(
        MedDbContext db,
        AuditTrailService audit,
        IWorkflowGuard<ProcedureVersion, string> workflow,
        ProcedureDocumentSnapshotService documents)
        : this(db, audit, workflow, documents, changeBus: null)
    {
    }

    public ProcedureLifecycleService(
        MedDbContext db,
        AuditTrailService audit,
        IWorkflowGuard<ProcedureVersion, string> workflow,
        ProcedureDocumentSnapshotService? documents,
        IMedDataChangeBus? changeBus)
    {
        _db = db;
        _audit = audit;
        _workflow = workflow;
        _documents = documents;
        _changeBus = changeBus;
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
        NotifyDataChanged();
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
        PersistVersionUpdate(updated);
        _documents?.PersistSnapshot(versionId, "submitted", submittedBy);
        _workflow.OnTransitioned(updated, ver.StatusCode, updated.StatusCode, submittedBy);

        _audit.Append(new AuditLog
        {
            CorrelationId = Guid.NewGuid(),
            ActorUserId = submittedBy,
            ActionCode = "submit",
            TargetType = "procedure_version",
            TargetId = versionId.ToString(),
            DepartmentId = updated.DepartmentId ?? procDepartmentId(ver.ProcedureId),
            MetadataJson = JsonSerializer.Serialize(new
            {
                Event = "procedure_submit",
                ver.ProcedureId,
                updated.ProcedureVersionId,
                updated.VersionLabel,
                VersionTitle = updated.Title,
                FromState = ver.StatusCode,
                ToState = updated.StatusCode
            })
        });
        NotifyDataChanged();
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

        var currentPublished = _db.ProcedureVersions.AsNoTracking()
            .Where(v => v.ProcedureId == ver.ProcedureId && v.StatusCode == "active")
            .ToList();

        foreach (var old in currentPublished)
        {
            var superseded = old with
            {
                StatusCode = "superseded",
                EffectiveTo = DateTime.UtcNow
            };
            PersistVersionUpdate(superseded);
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
        PersistVersionUpdate(published);
        _documents?.PersistSnapshot(versionId, "published", approvedBy);

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
            TargetId = versionId.ToString(),
            DepartmentId = published.DepartmentId ?? procDepartmentId(ver.ProcedureId),
            MetadataJson = JsonSerializer.Serialize(new
            {
                Event = "procedure_publish",
                ver.ProcedureId,
                published.ProcedureVersionId,
                published.VersionLabel,
                VersionTitle = published.Title,
                FromState = ver.StatusCode,
                ToState = published.StatusCode
            })
        });
        NotifyDataChanged();
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
        PersistVersionUpdate(rejected);
        _documents?.PersistSnapshot(versionId, "rejected", rejectedBy);
        _workflow.OnTransitioned(rejected, ver.StatusCode, rejected.StatusCode, rejectedBy, reason);

        _audit.Append(new AuditLog
        {
            CorrelationId = Guid.NewGuid(),
            ActorUserId = rejectedBy,
            ActionCode = "reject",
            TargetType = "procedure_version",
            TargetId = versionId.ToString(),
            DepartmentId = rejected.DepartmentId ?? procDepartmentId(ver.ProcedureId),
            MetadataJson = JsonSerializer.Serialize(new
            {
                Event = "procedure_reject",
                ver.ProcedureId,
                rejected.ProcedureVersionId,
                rejected.VersionLabel,
                VersionTitle = rejected.Title,
                FromState = ver.StatusCode,
                ToState = rejected.StatusCode,
                Reason = reason
            })
        });
        NotifyDataChanged();
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
        PersistVersionUpdate(withdrawn);
        _documents?.PersistSnapshot(versionId, "withdrawn", withdrawnBy);
        _workflow.OnTransitioned(withdrawn, ver.StatusCode, withdrawn.StatusCode, withdrawnBy, reason);

        _audit.Append(new AuditLog
        {
            CorrelationId = Guid.NewGuid(),
            ActorUserId = withdrawnBy,
            ActionCode = "revoke",
            TargetType = "procedure_version",
            TargetId = versionId.ToString(),
            DepartmentId = withdrawn.DepartmentId ?? procDepartmentId(ver.ProcedureId),
            MetadataJson = JsonSerializer.Serialize(new
            {
                Event = "procedure_withdraw",
                ver.ProcedureId,
                withdrawn.ProcedureVersionId,
                withdrawn.VersionLabel,
                VersionTitle = withdrawn.Title,
                FromState = ver.StatusCode,
                ToState = withdrawn.StatusCode,
                Reason = reason
            })
        });
        NotifyDataChanged();
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
        PersistVersionUpdate(archived);
        _documents?.PersistSnapshot(versionId, "archived", archivedBy);
        _workflow.OnTransitioned(archived, ver.StatusCode, archived.StatusCode, archivedBy, reason);

        _audit.Append(new AuditLog
        {
            CorrelationId = Guid.NewGuid(),
            ActorUserId = archivedBy,
            ActionCode = "archive",
            TargetType = "procedure_version",
            TargetId = versionId.ToString(),
            DepartmentId = archived.DepartmentId ?? procDepartmentId(ver.ProcedureId),
            MetadataJson = JsonSerializer.Serialize(new
            {
                Event = "procedure_archive",
                ver.ProcedureId,
                archived.ProcedureVersionId,
                archived.VersionLabel,
                VersionTitle = archived.Title,
                FromState = ver.StatusCode,
                ToState = archived.StatusCode,
                Reason = reason
            })
        });
        NotifyDataChanged();
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
        PersistVersionUpdate(restored);
        _documents?.PersistSnapshot(versionId, "restored", restoredBy);
        _workflow.OnTransitioned(restored, ver.StatusCode, restored.StatusCode, restoredBy, reason);

        _audit.Append(new AuditLog
        {
            CorrelationId = Guid.NewGuid(),
            ActorUserId = restoredBy,
            ActionCode = "restore",
            TargetType = "procedure_version",
            TargetId = versionId.ToString(),
            DepartmentId = restored.DepartmentId ?? procDepartmentId(ver.ProcedureId),
            MetadataJson = JsonSerializer.Serialize(new
            {
                Event = "procedure_restore",
                ver.ProcedureId,
                restored.ProcedureVersionId,
                restored.VersionLabel,
                VersionTitle = restored.Title,
                FromState = ver.StatusCode,
                ToState = restored.StatusCode,
                Reason = reason
            })
        });
        NotifyDataChanged();
    }

    public ProcedureVersion? GetActiveVersion(Guid procedureId)
    {
        return _db.ProcedureVersions
            .FirstOrDefault(v => v.ProcedureId == procedureId && v.StatusCode == "active");
    }

    /// <summary>Khôi phục phiên bản cũ (superseded/archived đã từng ban hành) thành bản đang hiệu lực.</summary>
    public void RollbackToVersion(Guid targetVersionId, Guid actorUserId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw MedDomainException.Constraint(
                "CK_procedure_version_rollback_reason",
                50030,
                "Phải nhập lý do khôi phục phiên bản.");
        }

        var target = GetVersionOrThrow(targetVersionId);
        var canRollbackTarget = string.Equals(target.StatusCode, "superseded", StringComparison.OrdinalIgnoreCase)
            || (string.Equals(target.StatusCode, "archived", StringComparison.OrdinalIgnoreCase) && target.PublishedAt.HasValue);
        if (!canRollbackTarget)
        {
            throw MedDomainException.Constraint(
                "CK_procedure_version_rollback_target",
                50031,
                "Chỉ có thể khôi phục phiên bản đã được thay thế hoặc đã từng ban hành rồi lưu trữ.");
        }

        var currentActive = _db.ProcedureVersions
            .FirstOrDefault(v =>
                v.ProcedureId == target.ProcedureId
                && v.StatusCode == "active"
                && v.ProcedureVersionId != targetVersionId);
        if (currentActive is null)
        {
            throw MedDomainException.Constraint(
                "CK_procedure_version_rollback_active",
                50032,
                "Không có phiên bản đang hiệu lực để thay thế khi khôi phục.");
        }

        EnsureTransition(currentActive, "archived", "CK_procedure_version_rollback", 50033,
            "Không thể lưu trữ phiên bản đang hiệu lực khi khôi phục.");
        EnsureTransition(target, "active", "CK_procedure_version_rollback", 50033,
            "Không thể khôi phục phiên bản từ trạng thái hiện tại.");

        var targetLabel = target.VersionLabel ?? $"v{target.VersionNo}";
        var activeLabel = currentActive.VersionLabel ?? $"v{currentActive.VersionNo}";
        var trimmedReason = reason.Trim();

        var archivedActive = currentActive with
        {
            StatusCode = "archived",
            EffectiveTo = DateTime.UtcNow,
            ChangeReason = $"Lưu trữ khi khôi phục {targetLabel}: {trimmedReason}"
        };
        var restored = target with
        {
            StatusCode = "active",
            EffectiveFrom = DateTime.UtcNow,
            EffectiveTo = null,
            ChangeReason = $"Khôi phục hiệu lực thay cho {activeLabel}: {trimmedReason}"
        };

        PersistVersionUpdate(archivedActive);
        PersistVersionUpdate(restored);

        _documents?.PersistSnapshot(currentActive.ProcedureVersionId, "superseded_by_rollback", actorUserId);
        _documents?.PersistSnapshot(targetVersionId, "rolled_back", actorUserId);

        _workflow.OnTransitioned(archivedActive, currentActive.StatusCode, archivedActive.StatusCode, actorUserId, trimmedReason);
        _workflow.OnTransitioned(restored, target.StatusCode, restored.StatusCode, actorUserId, trimmedReason);

        _audit.Append(new AuditLog
        {
            CorrelationId = Guid.NewGuid(),
            ActorUserId = actorUserId,
            ActionCode = "rollback",
            TargetType = "procedure_version",
            TargetId = targetVersionId.ToString(),
            DepartmentId = restored.DepartmentId ?? procDepartmentId(target.ProcedureId),
            MetadataJson = JsonSerializer.Serialize(new
            {
                Event = "procedure_rollback",
                target.ProcedureId,
                restored.ProcedureVersionId,
                restored.VersionLabel,
                VersionTitle = restored.Title,
                ReplacedVersionId = currentActive.ProcedureVersionId,
                ReplacedVersionLabel = currentActive.VersionLabel,
                Reason = trimmedReason
            })
        });
        NotifyDataChanged();
    }

    public bool CanRollbackToVersion(Guid targetVersionId, out string? reason)
    {
        reason = null;
        var target = _db.ProcedureVersions.FirstOrDefault(v => v.ProcedureVersionId == targetVersionId);
        if (target is null)
        {
            reason = "Phiên bản không tồn tại.";
            return false;
        }

        var canRollbackTarget = string.Equals(target.StatusCode, "superseded", StringComparison.OrdinalIgnoreCase)
            || (string.Equals(target.StatusCode, "archived", StringComparison.OrdinalIgnoreCase) && target.PublishedAt.HasValue);
        if (!canRollbackTarget)
        {
            reason = "Chỉ khôi phục được bản đã thay thế hoặc bản đã từng ban hành.";
            return false;
        }

        var hasOtherActive = _db.ProcedureVersions.Any(v =>
            v.ProcedureId == target.ProcedureId
            && v.StatusCode == "active"
            && v.ProcedureVersionId != targetVersionId);
        if (!hasOtherActive)
        {
            reason = "Không có phiên bản đang hiệu lực khác để thay thế.";
            return false;
        }

        return _workflow.CanTransition(target.StatusCode, "active");
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
        _db.ChangeTracker.Clear();
        return _db.ProcedureVersions.AsNoTracking()
                   .FirstOrDefault(v => v.ProcedureVersionId == versionId)
               ?? throw MedDomainException.Constraint("FK_procedure_version", 547,
                   "Phiên bản quy trình không tồn tại.");
    }

    private void PersistVersionUpdate(ProcedureVersion updated)
    {
        try
        {
            EfWriteHelper.UpdateProcedureVersion(_db, updated);
        }
        catch (InvalidOperationException exception)
        {
            throw MedDomainException.Constraint("FK_procedure_version", 547, exception.Message);
        }
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
        var writerUserIds = snapshot.Signoffs
            .Where(signoff => string.Equals(signoff.SignoffRole, "writer", StringComparison.OrdinalIgnoreCase)
                && string.Equals(signoff.ContentHashSha256, hash, StringComparison.OrdinalIgnoreCase)
                && signoff.SignerUserId.HasValue)
            .Select(signoff => signoff.SignerUserId!.Value)
            .ToHashSet();
        var checkerUserId = snapshot.Signoffs
            .Where(signoff => string.Equals(signoff.SignoffRole, "checker", StringComparison.OrdinalIgnoreCase)
                && string.Equals(signoff.ContentHashSha256, hash, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(signoff => signoff.SignedAt)
            .FirstOrDefault()?.SignerUserId;

        if (writerUserIds.Contains(approvedBy) || checkerUserId == approvedBy)
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

    private Guid? procDepartmentId(Guid procedureId)
        => _db.Procedures.FirstOrDefault(item => item.ProcedureId == procedureId)?.OwnerDepartmentId;

    private void NotifyDataChanged() => _changeBus?.Publish();
}
