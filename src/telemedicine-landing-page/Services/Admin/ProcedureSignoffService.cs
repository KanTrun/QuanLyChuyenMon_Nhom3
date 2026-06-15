using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin;

public sealed class ProcedureSignoffService
{
    public static readonly string[] RequiredRoles = ["writer", "checker", "approver"];
    private readonly IMedDataStore _store;
    private readonly ProcedureDocumentSnapshotService _snapshots;

    public ProcedureSignoffService(IMedDataStore store, ProcedureDocumentSnapshotService snapshots)
    {
        _store = store;
        _snapshots = snapshots;
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

        var normalizedRole = role.ToLowerInvariant();
        var snapshot = _snapshots.GetSnapshot(versionId);
        EnsureSigningStage(snapshot, normalizedRole);

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
            SignatureImageDataUrl = signatureImageDataUrl,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        };
        _store.AddProcedureSignoffRecord(signoff);
        return signoff;
    }

    public bool HasCurrentSignoff(Guid versionId, string role)
        => _snapshots.HasCurrentSignoff(_snapshots.GetSnapshot(versionId), role);

    private void EnsureSigningStage(ProcedureDocumentSnapshot snapshot, string role)
    {
        var expectedStatus = role == "writer" ? "draft" : "pending_approval";
        if (!string.Equals(snapshot.Version.StatusCode, expectedStatus, StringComparison.OrdinalIgnoreCase))
        {
            var stageLabel = role == "writer" ? "bản nháp" : "chờ phê duyệt";
            throw new InvalidOperationException($"Chỉ được ký {RoleLabel(role)} khi phiên bản ở trạng thái {stageLabel}.");
        }

        if (role is "checker" or "approver" && !_snapshots.HasCurrentSignoff(snapshot, "writer"))
            throw new InvalidOperationException("Chữ ký người viết không còn hợp lệ trên nội dung hiện tại.");

        if (role == "approver" && !_snapshots.HasCurrentSignoff(snapshot, "checker"))
            throw new InvalidOperationException("Người kiểm tra phải ký nội bộ trước khi người phê duyệt ký.");
    }

    private static string RoleLabel(string role) => role switch
    {
        "writer" => "người viết",
        "checker" => "người kiểm tra",
        "approver" => "người phê duyệt",
        _ => role
    };
}
