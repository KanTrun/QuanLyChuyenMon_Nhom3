using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin;

public sealed class ProcedureSignoffService
{
    private const int MaxSignatureImageBytes = 256 * 1024;
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
        var validatedSignatureImage = ValidateSignatureImage(signatureImageDataUrl);

        var normalizedRole = role.ToLowerInvariant();
        var snapshot = _snapshots.GetSnapshot(versionId);
        EnsureSigningStage(snapshot, normalizedRole);
        EnsureNotAlreadySigned(snapshot, normalizedRole);
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
        return signoff;
    }

    public bool HasCurrentSignoff(Guid versionId, string role)
        => _snapshots.HasCurrentSignoff(_snapshots.GetSnapshot(versionId), role);

    public Guid? GetCurrentSignerUserId(Guid versionId, string role)
    {
        var snapshot = _snapshots.GetSnapshot(versionId);
        var hash = _snapshots.ComputeContentHash(versionId);
        return snapshot.Signoffs
            .Where(signoff =>
                string.Equals(signoff.SignoffRole, role, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(signoff.ContentHashSha256, hash, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(signoff => signoff.SignedAt)
            .FirstOrDefault()?.SignerUserId;
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
            if (_snapshots.HasCurrentSignoff(snapshot, normalizedRole))
            {
                reason = $"Chữ ký {RoleLabel(normalizedRole)} đã được xác nhận trên nội dung hiện tại.";
                return false;
            }

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

    private void EnsureNotAlreadySigned(ProcedureDocumentSnapshot snapshot, string role)
    {
        if (_snapshots.HasCurrentSignoff(snapshot, role))
            throw new InvalidOperationException($"Chữ ký {RoleLabel(role)} đã được xác nhận trên nội dung hiện tại.");
    }

    private void EnsureSeparationOfDuties(ProcedureDocumentSnapshot snapshot, string role, Guid userId)
    {
        var writerUserId = GetCurrentSignerUserId(snapshot, "writer");
        var checkerUserId = GetCurrentSignerUserId(snapshot, "checker");

        if (role == "checker" && writerUserId == userId)
            throw new InvalidOperationException("Người kiểm tra phải là tài khoản khác người viết.");

        if (role == "approver")
        {
            if (writerUserId == userId)
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
                string.Equals(signoff.SignoffRole, role, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(signoff.ContentHashSha256, hash, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(signoff => signoff.SignedAt)
            .FirstOrDefault()?.SignerUserId;
    }

    private static string RoleLabel(string role) => role switch
    {
        "writer" => "người viết",
        "checker" => "người kiểm tra",
        "approver" => "người phê duyệt",
        _ => role
    };

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
}
