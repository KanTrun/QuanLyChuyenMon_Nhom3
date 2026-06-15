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

        var normalizedRole = role.ToLowerInvariant();
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
}
