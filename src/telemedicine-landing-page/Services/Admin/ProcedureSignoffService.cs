using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;
using System.Text.Json;

namespace TelemedicineLandingPage.Services.Admin;

public sealed class ProcedureSignoffService
{
    private const int MaxSignatureImageBytes = 256 * 1024;
    public static readonly string[] RequiredRoles = ["writer", "checker", "approver"];
    private readonly IMedDataStore _store;
    private readonly ProcedureDocumentSnapshotService _snapshots;
    private readonly AuditTrailService? _audit;

    public ProcedureSignoffService(IMedDataStore store, ProcedureDocumentSnapshotService snapshots, AuditTrailService? audit = null)
    {
        _store = store;
        _snapshots = snapshots;
        _audit = audit;
    }

    public ProcedureSignoffRecord Sign(
        Guid versionId,
        string role,
        Guid? userId,
        string? username,
        string? fullName,
        string? signatureImageDataUrl = null,
        string? note = null)
    {
        if (!RequiredRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("Vai trò ký không hợp lệ.");
        if (userId is null || userId == Guid.Empty)
            throw new InvalidOperationException("Chữ ký nội bộ phải gắn với một tài khoản người dùng hợp lệ.");
        var validatedSignatureImage = ValidateSignatureImage(signatureImageDataUrl);

        if (!_store.IsProcedureWriteBatchActive)
            _store.Refresh();
        else
            // Đảm bảo nội dung soạn thảo đã ghi DB trước khi băm — tránh "chữ ký đã cũ" ngay sau khi ký.
            _store.FlushProcedureWriteBatchPendingChanges();
        var normalizedRole = role.ToLowerInvariant();
        var snapshot = _snapshots.GetSnapshot(versionId);
        EnsureSigningStage(snapshot, normalizedRole);
        EnsureNotAlreadySigned(snapshot, normalizedRole, userId.Value);
        EnsureSeparationOfDuties(snapshot, normalizedRole, userId.Value);

        var signoff = new ProcedureSignoffRecord
        {
            ProcedureVersionId = versionId,
            SignoffRole = normalizedRole,
            DisplayOrder = Array.IndexOf(RequiredRoles, normalizedRole) + 1,
            SignerUserId = userId,
            SignerUsername = username,
            SignerFullName = fullName,
            SignedAt = DateTime.UtcNow,
            ContentHashSha256 = _snapshots.ComputeContentHash(versionId),
            SignatureImageDataUrl = validatedSignatureImage,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        };
        _store.AddProcedureSignoffRecord(signoff);
        if (!_store.IsProcedureWriteBatchActive)
        {
            AppendProcedureSignoffAudit(snapshot, signoff);
            // Thông báo cho các người viết khác khi có ai đó ký (best-effort)
            try { NotifyOtherWriters(snapshot, signoff, fullName ?? username); } catch { }
        }
        return signoff;
    }

    public void RecordSignoffAudit(Guid versionId, ProcedureSignoffRecord signoff)
    {
        var snapshot = _snapshots.GetSnapshot(versionId);
        AppendProcedureSignoffAudit(snapshot, signoff);
    }

    public bool HasCurrentSignoff(Guid versionId, string role)
        => _snapshots.HasCurrentSignoff(_snapshots.GetSnapshot(versionId), role);

    public int GetOutstandingWriterSignatures(Guid versionId)
    {
        var snapshot = _snapshots.GetSnapshot(versionId);
        var required = _snapshots.RequiredSignoffCount(snapshot, "writer");
        var effective = _snapshots.GetEffectiveWriterSignatureCount(snapshot);
        return Math.Max(0, required - effective);
    }

    public Guid? GetCurrentSignerUserId(Guid versionId, string role)
    {
        var snapshot = _snapshots.GetSnapshot(versionId);
        var hash = _snapshots.ComputeContentHash(versionId);
        return snapshot.Signoffs
            .Where(signoff =>
                !signoff.IsRevoked &&
                string.Equals(signoff.SignoffRole, role, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(signoff.ContentHashSha256, hash, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(signoff => signoff.SignedAt)
            .FirstOrDefault()?.SignerUserId;
    }

    /// <summary>
    /// Thu hồi chữ ký nội bộ của một vai trò trên phiên bản quy trình.
    /// Logic:
    /// - writer (status=draft): bản thân writer có thể hủy chữ ký của mình.
    /// - checker (status=pending_approval): checker tự hủy, hoặc approver hủy chữ ký checker để yêu cầu ký lại.
    /// - approver: không có chữ ký riêng biệt (ký xong là ban hành ngay), không cần thu hồi.
    /// </summary>
    public void RevokeSignoff(Guid versionId, Guid signoffRecordId, Guid revokedByUserId, string? reason = null)
    {
        if (revokedByUserId == Guid.Empty)
            throw new InvalidOperationException("Người thu hồi chữ ký phải là tài khoản hợp lệ.");

        if (!_store.IsProcedureWriteBatchActive)
            _store.Refresh();

        var snapshot = _snapshots.GetSnapshot(versionId);
        var signoff = snapshot.Signoffs.FirstOrDefault(s => s.ProcedureSignoffRecordId == signoffRecordId)
            ?? throw new InvalidOperationException("Bản ghi chữ ký không tồn tại trên phiên bản này.");

        if (signoff.IsRevoked)
            throw new InvalidOperationException("Chữ ký này đã được thu hồi trước đó.");

        EnsureRevokePermission(snapshot, signoff, revokedByUserId);

        _store.RevokeProcedureSignoffRecord(signoffRecordId, revokedByUserId, reason);

        if (_audit is not null)
        {
            _audit.Append(new AuditLog
            {
                CorrelationId = Guid.NewGuid(),
                ActorUserId = revokedByUserId,
                ActorUsername = snapshot.Signoffs.FirstOrDefault()?.SignerUsername,
                ActionCode = "revoke_signoff",
                TargetType = "procedure_version",
                TargetId = versionId.ToString(),
                DepartmentId = snapshot.Version.DepartmentId ?? snapshot.Procedure.OwnerDepartmentId,
                MetadataJson = JsonSerializer.Serialize(new
                {
                    Event = "procedure_signoff_revoked",
                    signoff.SignoffRole,
                    RoleLabel = RoleLabel(signoff.SignoffRole),
                    snapshot.Procedure.ProcedureId,
                    snapshot.Procedure.ProcedureCode,
                    ProcedureName = snapshot.Procedure.Name,
                    snapshot.Version.ProcedureVersionId,
                    snapshot.Version.VersionLabel,
                    VersionTitle = snapshot.Version.Title,
                    OriginalSignerUserId = signoff.SignerUserId,
                    OriginalSignerName = signoff.SignerFullName ?? signoff.SignerUsername,
                    Reason = reason
                })
            });
        }
    }

    public bool CanRevokeSignoff(Guid versionId, Guid signoffRecordId, Guid revokedByUserId, out string? reason)
    {
        reason = null;
        if (revokedByUserId == Guid.Empty)
        {
            reason = "Phải đăng nhập để thu hồi chữ ký.";
            return false;
        }

        try
        {
            var snapshot = _snapshots.GetSnapshot(versionId);
            var signoff = snapshot.Signoffs.FirstOrDefault(s => s.ProcedureSignoffRecordId == signoffRecordId);
            if (signoff is null)
            {
                reason = "Bản ghi chữ ký không tồn tại.";
                return false;
            }
            if (signoff.IsRevoked)
            {
                reason = "Chữ ký này đã được thu hồi trước đó.";
                return false;
            }
            EnsureRevokePermission(snapshot, signoff, revokedByUserId);
            return true;
        }
        catch (InvalidOperationException ex)
        {
            reason = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Lấy chữ ký đang có hiệu lực (chưa bị thu hồi, khớp hash hiện tại) của một người viết cụ thể.
    /// </summary>
    public ProcedureSignoffRecord? GetActiveWriterSignoff(Guid versionId, Guid writerUserId)
    {
        var snapshot = _snapshots.GetSnapshot(versionId);
        var hash = _snapshots.ComputeContentHash(versionId);
        return snapshot.Signoffs
            .Where(s =>
                !s.IsRevoked &&
                string.Equals(s.SignoffRole, "writer", StringComparison.OrdinalIgnoreCase) &&
                s.SignerUserId == writerUserId &&
                string.Equals(s.ContentHashSha256, hash, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(s => s.SignedAt)
            .FirstOrDefault();
    }

    /// <summary>
    /// Kiểm tra xem phiên bản có từ 2 người viết khác nhau đã ký hợp lệ hay không.
    /// Dùng để quyết định có hiển thị nút "Hoàn trả về người viết cuối" riêng biệt không.
    /// </summary>
    public bool HasMultipleWriterSignoffs(Guid versionId)
    {
        var snapshot = _snapshots.GetSnapshot(versionId);
        var hash = _snapshots.ComputeContentHash(versionId);
        return snapshot.Signoffs
            .Where(s => !s.IsRevoked &&
                        string.Equals(s.SignoffRole, "writer", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(s.ContentHashSha256, hash, StringComparison.OrdinalIgnoreCase))
            .Select(s => s.SignerUserId)
            .Distinct()
            .Count() > 1;
    }

    /// <summary>
    /// Kiểm tra xem user hiện tại có phải là người viết đã ký (hợp lệ) trên phiên bản không.
    /// </summary>
    public bool IsCurrentUserAWriter(Guid versionId, Guid userId)
    {
        if (userId == Guid.Empty) return false;
        var snapshot = _snapshots.GetSnapshot(versionId);
        var hash = _snapshots.ComputeContentHash(versionId);
        return snapshot.Signoffs.Any(s =>
            !s.IsRevoked &&
            string.Equals(s.SignoffRole, "writer", StringComparison.OrdinalIgnoreCase) &&
            s.SignerUserId == userId &&
            string.Equals(s.ContentHashSha256, hash, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Lấy chữ ký đang có hiệu lực (chưa bị thu hồi, khớp hash hiện tại) của checker/approver.
    /// </summary>
    public ProcedureSignoffRecord? GetActiveSignoff(Guid versionId, string role)
    {
        var snapshot = _snapshots.GetSnapshot(versionId);
        var hash = _snapshots.ComputeContentHash(versionId);
        return snapshot.Signoffs
            .Where(s =>
                !s.IsRevoked &&
                string.Equals(s.SignoffRole, role, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(s.ContentHashSha256, hash, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(s => s.SignedAt)
            .FirstOrDefault();
    }

    private void EnsureRevokePermission(ProcedureDocumentSnapshot snapshot, ProcedureSignoffRecord signoff, Guid revokedByUserId)
    {
        var role = signoff.SignoffRole.ToLowerInvariant();
        var versionStatus = snapshot.Version.StatusCode;

        switch (role)
        {
            case "writer":
                // Writer có thể tự hủy chữ ký khi ở draft, pending_review, hoặc pending_approval (nếu checker chưa ký)
                var writerAllowedStatuses = new[] { "draft", "pending_review", "pending_approval" };
                if (!writerAllowedStatuses.Contains(versionStatus, StringComparer.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        "Chỉ thu hồi chữ ký người viết khi phiên bản đang ở trạng thái bản nháp, chờ kiểm tra hoặc chờ phê duyệt.");
                if (signoff.SignerUserId != revokedByUserId)
                    throw new InvalidOperationException("Người viết chỉ được thu hồi chữ ký của chính mình.");
                // Chỉ thu hồi khi checker chưa ký
                if (_snapshots.HasCurrentSignoff(snapshot, "checker"))
                    throw new InvalidOperationException(
                        "Không thể thu hồi chữ ký người viết khi người kiểm tra đã ký. Hãy yêu cầu người kiểm tra hoặc người phê duyệt hoàn trả về soạn thảo.");
                break;

            case "checker":
                // Checker tự hủy hoặc approver-level hủy; từ pending_review hoặc pending_approval
                var checkerAllowedStatuses = new[] { "pending_review", "pending_approval" };
                if (!checkerAllowedStatuses.Contains(versionStatus, StringComparer.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        "Chỉ thu hồi chữ ký người kiểm tra khi phiên bản đang chờ kiểm tra hoặc chờ phê duyệt.");
                var writerIds = GetCurrentSignerUserIds(snapshot, "writer");
                var isWriter = writerIds.Contains(revokedByUserId);
                if (isWriter && signoff.SignerUserId != revokedByUserId)
                    throw new InvalidOperationException("Người viết không được thu hồi chữ ký của người kiểm tra.");
                break;

            default:
                throw new InvalidOperationException($"Không hỗ trợ thu hồi chữ ký vai trò '{RoleLabel(role)}'.");
        }
    }

    private IReadOnlySet<Guid> GetCurrentSignerUserIds(ProcedureDocumentSnapshot snapshot, string role)
    {
        if (string.Equals(role, "writer", StringComparison.OrdinalIgnoreCase))
        {
            return _snapshots.GetOrderedWriterAssignments(snapshot)
                .Where(assignment => _snapshots.IsWriterEffectivelySigned(snapshot, assignment.AssignedUserId))
                .Select(assignment => assignment.AssignedUserId)
                .ToHashSet();
        }

        var hash = _snapshots.ComputeContentHash(snapshot.Version.ProcedureVersionId);
        return snapshot.Signoffs
            .Where(signoff =>
                !signoff.IsRevoked &&
                string.Equals(signoff.SignoffRole, role, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(signoff.ContentHashSha256, hash, StringComparison.OrdinalIgnoreCase) &&
                signoff.SignerUserId.HasValue)
            .Select(signoff => signoff.SignerUserId!.Value)
            .ToHashSet();
    }

    public bool CanUserEditDraft(Guid versionId, Guid userId, out string? reason)
    {
        reason = null;
        if (userId == Guid.Empty)
        {
            reason = "Chỉ người viết được phân công mới có thể chỉnh sửa bản nháp.";
            return false;
        }

        try
        {
            var snapshot = _snapshots.GetSnapshot(versionId);
            if (!string.Equals(snapshot.Version.StatusCode, "draft", StringComparison.OrdinalIgnoreCase))
            {
                reason = "Chỉ có thể chỉnh sửa phiên bản ở trạng thái bản nháp.";
                return false;
            }

            var assignedWriterIds = snapshot.WriterAssignments
                .Where(item => string.Equals(item.SignoffRole, "writer", StringComparison.OrdinalIgnoreCase))
                .Select(item => item.AssignedUserId)
                .Distinct()
                .ToList();
            if (assignedWriterIds.Count == 0 || !assignedWriterIds.Contains(userId))
            {
                reason = "Tài khoản hiện tại không nằm trong danh sách người viết được phân công.";
                return false;
            }

            if (_snapshots.IsWriterEffectivelySigned(snapshot, userId))
            {
                reason = "Bạn đã ký trên nội dung hiện tại. Mở chế độ xem & ký nếu cần rà soát lại.";
                return false;
            }

            var writerAssignment = snapshot.WriterAssignments
                .FirstOrDefault(item =>
                    string.Equals(item.SignoffRole, "writer", StringComparison.OrdinalIgnoreCase) &&
                    item.AssignedUserId == userId);
            var maxOrderOnCurrentHash = _snapshots.GetMaxWriterDisplayOrderOnCurrentHash(snapshot);
            if (writerAssignment is not null &&
                maxOrderOnCurrentHash > 0 &&
                writerAssignment.DisplayOrder < maxOrderOnCurrentHash)
            {
                reason = "Người viết phía sau đã ký trên nội dung hiện tại. Mở chế độ xem & ký nếu cần rà soát lại.";
                return false;
            }

            return true;
        }
        catch (InvalidOperationException exception)
        {
            reason = exception.Message;
            return false;
        }
    }

    public bool CanUserSign(Guid versionId, string role, Guid userId, out string? reason)
    {
        reason = null;
        if (userId == Guid.Empty)
        {
            reason = "Chữ ký nội bộ phải gắn với một tài khoản người dùng hợp lệ.";
            return false;
        }

        try
        {
            var snapshot = _snapshots.GetSnapshot(versionId);
            var normalizedRole = role.ToLowerInvariant();
            EnsureSigningStage(snapshot, normalizedRole);
            EnsureNotAlreadySigned(snapshot, normalizedRole, userId);
            EnsureSeparationOfDuties(snapshot, normalizedRole, userId);
            return true;
        }
        catch (InvalidOperationException exception)
        {
            reason = exception.Message;
            return false;
        }
    }

    public static bool IsValidSignatureImage(string? value)
    {
        try
        {
            _ = ValidateSignatureImage(value);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private void EnsureSigningStage(ProcedureDocumentSnapshot snapshot, string role)
    {
        var status = snapshot.Version.StatusCode;
        switch (role.ToLowerInvariant())
        {
            case "writer":
                if (!string.Equals(status, "draft", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Chỉ được ký người viết khi phiên bản đang ở trạng thái bản nháp.");
                break;

            case "checker":
                // Ký kiểm tra tại pending_review (luồng mới) hoặc pending_approval (tương thích dữ liệu cũ)
                if (!string.Equals(status, "pending_review", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(status, "pending_approval", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        "Chỉ được ký kiểm tra khi phiên bản đang ở trạng thái chờ kiểm tra hoặc chờ phê duyệt.");
                break;

            case "approver":
                if (!string.Equals(status, "pending_approval", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        "Chỉ được ký phê duyệt khi phiên bản đang ở trạng thái chờ phê duyệt.");
                break;
        }

        if (role is "checker" or "approver" && !_snapshots.HasCurrentSignoff(snapshot, "writer"))
            throw new InvalidOperationException("Chữ ký người viết không còn hợp lệ trên nội dung hiện tại.");

        if (role == "approver" && !_snapshots.HasCurrentSignoff(snapshot, "checker"))
            throw new InvalidOperationException("Người kiểm tra phải ký nội bộ trước khi người phê duyệt ký.");
    }

    private void EnsureNotAlreadySigned(ProcedureDocumentSnapshot snapshot, string role, Guid userId)
    {
        if (string.Equals(role, "writer", StringComparison.OrdinalIgnoreCase))
        {
            if (_snapshots.IsWriterEffectivelySigned(snapshot, userId))
                throw new InvalidOperationException($"Chữ ký {RoleLabel(role)} đã được xác nhận trên nội dung hiện tại.");
            return;
        }

        if (HasUserCurrentSignoff(snapshot, role, userId))
            throw new InvalidOperationException($"Chữ ký {RoleLabel(role)} đã được xác nhận trên nội dung hiện tại.");
        if (_snapshots.HasCurrentSignoff(snapshot, role))
            throw new InvalidOperationException($"Chữ ký {RoleLabel(role)} đã được xác nhận trên nội dung hiện tại.");
    }

    private void EnsureSeparationOfDuties(ProcedureDocumentSnapshot snapshot, string role, Guid userId)
    {
        var writerUserIds = GetCurrentSignerUserIds(snapshot, "writer");
        var checkerUserId = GetCurrentSignerUserId(snapshot, "checker");

        if (role == "writer")
        {
            var writerAssignments = snapshot.WriterAssignments
                .Where(item => string.Equals(item.SignoffRole, "writer", StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.DisplayOrder)
                .ToList();
            var assignedWriterIds = writerAssignments.Select(item => item.AssignedUserId).Distinct().ToList();
            if (assignedWriterIds.Count > 0 && !assignedWriterIds.Contains(userId))
                throw new InvalidOperationException("Tài khoản hiện tại không nằm trong danh sách người viết được phân công.");

            // Kiểm tra thứ tự ký: tất cả người viết có thứ tự thấp hơn phải có ít nhất 1 chữ ký chưa bị thu hồi.
            // Dùng HasAnyActiveWriterSignoff (không kiểm tra hash) vì Writer 2 có thể sửa nội dung
            // làm hash thay đổi, khiến chữ ký Writer 1 trở thành "cũ" — nhưng thứ tự vẫn được đảm bảo.
            var myAssignment = writerAssignments.FirstOrDefault(w => w.AssignedUserId == userId);
            if (myAssignment is not null && myAssignment.DisplayOrder > writerAssignments.Min(w => w.DisplayOrder))
            {
                var unsignedPriorWriters = writerAssignments
                    .Where(w => w.DisplayOrder < myAssignment.DisplayOrder &&
                                !snapshot.Signoffs.Any(s =>
                                    !s.IsRevoked &&
                                    string.Equals(s.SignoffRole, "writer", StringComparison.OrdinalIgnoreCase) &&
                                    s.SignerUserId == w.AssignedUserId))
                    .ToList();
                if (unsignedPriorWriters.Count > 0)
                {
                    var minUnsignedOrder = unsignedPriorWriters.Min(w => w.DisplayOrder);
                    throw new InvalidOperationException(
                        $"Người viết thứ {minUnsignedOrder} trong danh sách chưa ký. Vui lòng ký theo thứ tự phân công.");
                }
            }
        }

        if (role == "checker" && writerUserIds.Contains(userId))
            throw new InvalidOperationException("Người kiểm tra phải là tài khoản khác người viết.");

        if (role == "approver")
        {
            if (writerUserIds.Contains(userId))
                throw new InvalidOperationException("Người phê duyệt phải khác người viết.");
            if (checkerUserId == userId)
                throw new InvalidOperationException("Người phê duyệt phải khác người kiểm tra.");
        }
    }

    private Guid? GetCurrentSignerUserId(ProcedureDocumentSnapshot snapshot, string role)
    {
        var hash = _snapshots.ComputeContentHash(snapshot.Version.ProcedureVersionId);
        return snapshot.Signoffs
            .Where(signoff =>
                !signoff.IsRevoked &&
                string.Equals(signoff.SignoffRole, role, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(signoff.ContentHashSha256, hash, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(signoff => signoff.SignedAt)
            .FirstOrDefault()?.SignerUserId;
    }

    private bool HasUserCurrentSignoff(ProcedureDocumentSnapshot snapshot, string role, Guid userId)
    {
        var hash = _snapshots.ComputeContentHash(snapshot.Version.ProcedureVersionId);
        return snapshot.Signoffs.Any(signoff =>
            !signoff.IsRevoked &&
            string.Equals(signoff.SignoffRole, role, StringComparison.OrdinalIgnoreCase) &&
            signoff.SignerUserId == userId &&
            string.Equals(signoff.ContentHashSha256, hash, StringComparison.OrdinalIgnoreCase));
    }

    private static string RoleLabel(string role) => role switch
    {
        "writer" => "người viết",
        "checker" => "người kiểm tra",
        "approver" => "người phê duyệt",
        _ => role
    };

    private void AppendProcedureSignoffAudit(ProcedureDocumentSnapshot snapshot, ProcedureSignoffRecord signoff)
    {
        if (_audit is null)
            return;

        _audit.Append(new AuditLog
        {
            CorrelationId = Guid.NewGuid(),
            ActorUserId = signoff.SignerUserId,
            ActorUsername = signoff.SignerUsername,
            ActionCode = "sign",
            TargetType = "procedure_version",
            TargetId = signoff.ProcedureVersionId.ToString(),
            DepartmentId = snapshot.Version.DepartmentId ?? snapshot.Procedure.OwnerDepartmentId,
            MetadataJson = JsonSerializer.Serialize(new
            {
                Event = "procedure_signoff",
                signoff.SignoffRole,
                RoleLabel = RoleLabel(signoff.SignoffRole),
                snapshot.Procedure.ProcedureId,
                snapshot.Procedure.ProcedureCode,
                ProcedureName = snapshot.Procedure.Name,
                snapshot.Version.ProcedureVersionId,
                snapshot.Version.VersionLabel,
                VersionTitle = snapshot.Version.Title,
                signoff.Note
            })
        });
    }

    private static string ValidateSignatureImage(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("Vui lòng ký trực tiếp trong khung chữ ký trước khi xác nhận.");

        var separator = value.IndexOf(',');
        if (separator <= 0)
            throw new InvalidOperationException("Dữ liệu chữ ký không hợp lệ.");

        var mediaType = value[..separator].ToLowerInvariant();
        if (mediaType is not ("data:image/png;base64" or "data:image/jpeg;base64" or "data:image/webp;base64"))
            throw new InvalidOperationException("Chữ ký chỉ hỗ trợ ảnh PNG, JPEG hoặc WebP.");

        var encoded = value[(separator + 1)..];
        if (encoded.Length > ((MaxSignatureImageBytes + 2) / 3) * 4)
            throw new InvalidOperationException("Ảnh chữ ký vượt quá dung lượng cho phép.");

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(encoded);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("Dữ liệu chữ ký không hợp lệ.");
        }

        if (bytes.Length == 0 || bytes.Length > MaxSignatureImageBytes || !MatchesImageSignature(mediaType, bytes))
            throw new InvalidOperationException("Dữ liệu ảnh chữ ký không hợp lệ.");

        return value;
    }

    private static bool MatchesImageSignature(string mediaType, byte[] bytes)
        => mediaType switch
        {
            "data:image/png;base64" => bytes.Length >= 8 &&
                bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
                bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A,
            "data:image/jpeg;base64" => bytes.Length >= 3 &&
                bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
            "data:image/webp;base64" => bytes.Length >= 12 &&
                bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
                bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50,
            _ => false
        };

    /// <summary>
    /// Thông báo nội bộ cho các người viết KHÁC khi có người ký, và người kiểm tra khi người viết cuối ký.
    /// </summary>
    private void NotifyOtherWriters(ProcedureDocumentSnapshot snapshot, ProcedureSignoffRecord signoff, string? signerName)
    {
        var versionId = snapshot.Version.ProcedureVersionId;
        var versionLabel = snapshot.Version.VersionLabel ?? $"v{snapshot.Version.VersionNo}";
        var roleLabel = signoff.SignoffRole switch
        {
            "writer"  => "Người viết",
            "checker" => "Người kiểm tra",
            "approver"=> "Người phê duyệt",
            _ => signoff.SignoffRole
        };
        var title = $"{signerName ?? "Ai đó"} đã ký ({roleLabel}) — {versionLabel}";
        var body  = $"{signerName ?? "Người dùng"} vừa ký xác nhận vai trò {roleLabel} trên quy trình \"{snapshot.Procedure.Name ?? versionLabel}\".";
        var payload = JsonSerializer.Serialize(new { versionId });

        // Notify other assigned writers
        var assignedWriterIds = snapshot.WriterAssignments
            .Where(w => string.Equals(w.SignoffRole, "writer", StringComparison.OrdinalIgnoreCase)
                     && w.AssignedUserId != signoff.SignerUserId)
            .Select(w => w.AssignedUserId)
            .Distinct();

        foreach (var uid in assignedWriterIds.Where(id => id != Guid.Empty))
        {
            _store.AddNotification(new MedNotification
            {
                RecipientUserId = uid,
                NotificationType = "procedure_signed",
                Title = title,
                Body = body,
                Severity = "info",
                SourceType = "procedure_version",
                SourceId = versionId.ToString(),
                PayloadJson = payload
            });
        }
    }
}
