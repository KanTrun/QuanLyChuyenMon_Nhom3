using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TelemedicineLandingPage.Application.Workflow;
using TelemedicineLandingPage.Data;
using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Application.Signature;

public sealed class SignatureService : ISignatureService
{
    public const string PatientProtocolApplicationTarget = "patient_protocol_application";
    public const string InternalProviderCode = "internal";
    public const int MaxSignatureImageBytes = 256 * 1024;
    public const int MaxMetadataJsonChars = 384 * 1024;
    private const string PngDataUrlPrefix = "data:image/png;base64,";
    private const string SignPermission = "SCR_CLINICAL:SIGN_PROTOCOL_APPLICATION";
    private const string RevokePermission = "SCR_ADMIN:MANAGE_SIGNATURES";
    private static readonly byte[] PngHeader = [137, 80, 78, 71, 13, 10, 26, 10];
    private static readonly string[] SignPermissionAliases =
    [
        SignPermission,
        "SCR_CLINICAL:EXECUTE",
        "PERM_CLINICAL_execute"
    ];

    private readonly IDbContextFactory<MedDbContext> _dbFactory;
    private readonly EffectivePermissionResolver _permissions;
    private readonly IWorkflowGuard<PatientProtocolApplication, string> _workflow;
    private readonly IMedDataChangeBus? _changeBus;

    public SignatureService(
        IDbContextFactory<MedDbContext> dbFactory,
        EffectivePermissionResolver permissions,
        IWorkflowGuard<PatientProtocolApplication, string> workflow,
        IMedDataChangeBus? changeBus = null)
    {
        _dbFactory = dbFactory;
        _permissions = permissions;
        _workflow = workflow;
        _changeBus = changeBus;
    }

    public async Task<(SignatureResult Result, SignatureRecord? Record)> CreateInternalSignatureAsync(
        string targetType,
        Guid targetId,
        Guid signerUserId,
        string signerUsername,
        string? metadataJson = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsSupportedTarget(targetType))
            return (SignatureResult.TargetNotFound, null);

        if (!CanSign(signerUserId, signerUsername))
            return (SignatureResult.Unauthorized, null);

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var existing = await GetSignatureAsync(db, targetType, targetId, cancellationToken);
        if (existing is not null)
            return (SignatureResult.AlreadySigned, existing);

        var target = await db.PatientProtocolApplications
            .FirstOrDefaultAsync(a => a.PatientProtocolApplicationId == targetId, cancellationToken);
        if (target is null)
            return (SignatureResult.TargetNotFound, null);

        if (!_workflow.CanTransition(target.ApplicationStatus, "signed"))
            return (SignatureResult.InvalidState, null);

        var validatedMetadataJson = ValidateMetadata(metadataJson);
        var signedAt = DateTime.UtcNow;
        var correlationId = Guid.NewGuid();
        var record = new SignatureRecord
        {
            TargetType = targetType,
            TargetId = targetId,
            SignerUserId = signerUserId,
            SignerUsername = signerUsername,
            ProviderCode = InternalProviderCode,
            IsLegallyValid = false,
            SignedAt = signedAt,
            SignatureHash = ComputeHash(targetType, targetId, signerUserId, signedAt, InternalProviderCode, validatedMetadataJson),
            MetadataJson = validatedMetadataJson,
            CorrelationId = correlationId
        };

        db.SignatureRecords.Add(record);
        db.PatientProtocolApplications.Entry(target).CurrentValues.SetValues(target with
        {
            ApplicationStatus = "signed",
            AppliedAt = target.AppliedAt ?? signedAt
        });
        db.AuditLogs.Add(new AuditLog
        {
            CorrelationId = correlationId,
            ActorUserId = signerUserId,
            ActorUsername = signerUsername,
            ActionCode = "sign",
            TargetType = targetType,
            TargetId = targetId.ToString(),
            MetadataJson = JsonSerializer.Serialize(new
            {
                ProviderCode = InternalProviderCode,
                IsLegallyValid = false
            })
        });

        await db.SaveChangesAsync(cancellationToken);
        _changeBus?.Publish();
        return (SignatureResult.Created, record);
    }

    public async Task<SignatureRecord?> GetSignatureAsync(
        string targetType,
        Guid targetId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await GetSignatureAsync(db, targetType, targetId, cancellationToken);
    }

    public async Task<SignatureResult> RevokeInternalSignatureAsync(
        string targetType,
        Guid targetId,
        Guid actorUserId,
        string actorUsername,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Ly do thu hoi chu ky la bat buoc.");
        if (!IsSupportedTarget(targetType))
            return SignatureResult.TargetNotFound;
        if (!CanRevoke(actorUserId, actorUsername))
            return SignatureResult.Unauthorized;

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var target = await db.PatientProtocolApplications
            .FirstOrDefaultAsync(a => a.PatientProtocolApplicationId == targetId, cancellationToken);
        if (target is null)
            return SignatureResult.TargetNotFound;
        if (!_workflow.CanTransition(target.ApplicationStatus, "revoked"))
            return SignatureResult.InvalidState;

        var correlationId = Guid.NewGuid();
        db.PatientProtocolApplications.Entry(target).CurrentValues.SetValues(target with
        {
            ApplicationStatus = "revoked"
        });
        db.AuditLogs.Add(new AuditLog
        {
            CorrelationId = correlationId,
            ActorUserId = actorUserId,
            ActorUsername = actorUsername,
            ActionCode = "revoke",
            TargetType = targetType,
            TargetId = targetId.ToString(),
            MetadataJson = JsonSerializer.Serialize(new { Reason = reason.Trim(), ProviderCode = InternalProviderCode })
        });

        await db.SaveChangesAsync(cancellationToken);
        _changeBus?.Publish();
        return SignatureResult.Revoked;
    }

    public bool VerifyIntegrity(SignatureRecord record)
        => HasValidIntegrity(record);

    public static bool HasValidIntegrity(SignatureRecord record)
    {
        var metadataBoundHash = ComputeHash(
            record.TargetType,
            record.TargetId,
            record.SignerUserId,
            record.SignedAt,
            record.ProviderCode,
            record.MetadataJson);
        if (string.Equals(record.SignatureHash, metadataBoundHash, StringComparison.OrdinalIgnoreCase))
            return true;

        return string.Equals(
            record.SignatureHash,
            ComputeLegacyHash(record.TargetType, record.TargetId, record.SignerUserId, record.SignedAt, record.ProviderCode),
            StringComparison.OrdinalIgnoreCase);
    }

    private static Task<SignatureRecord?> GetSignatureAsync(
        MedDbContext db,
        string targetType,
        Guid targetId,
        CancellationToken cancellationToken)
        => db.SignatureRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TargetType == targetType && s.TargetId == targetId, cancellationToken);

    private bool CanSign(Guid signerUserId, string signerUsername)
        => IsAdmin(signerUsername) || SignPermissionAliases.Any(permission => _permissions.HasPermission(signerUserId, permission));

    private bool CanRevoke(Guid actorUserId, string actorUsername)
        => IsAdmin(actorUsername) || _permissions.HasPermission(actorUserId, RevokePermission);

    private static bool IsAdmin(string username)
        => string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase);

    private static bool IsSupportedTarget(string targetType)
        => string.Equals(targetType, PatientProtocolApplicationTarget, StringComparison.Ordinal);

    private static string ComputeHash(
        string targetType,
        Guid targetId,
        Guid signerUserId,
        DateTime signedAt,
        string providerCode,
        string? metadataJson)
        => Hash($"{LegacyPayload(targetType, targetId, signerUserId, signedAt, providerCode)}:{metadataJson ?? string.Empty}");

    private static string ComputeLegacyHash(
        string targetType,
        Guid targetId,
        Guid signerUserId,
        DateTime signedAt,
        string providerCode)
        => Hash(LegacyPayload(targetType, targetId, signerUserId, signedAt, providerCode));

    private static string LegacyPayload(
        string targetType,
        Guid targetId,
        Guid signerUserId,
        DateTime signedAt,
        string providerCode)
        => $"{targetType}:{targetId}:{signerUserId}:{signedAt:O}:{providerCode}";

    private static string Hash(string payload)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();

    private static string? ValidateMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
            return null;
        if (metadataJson.Length > MaxMetadataJsonChars)
            throw new InvalidOperationException("Metadata chu ky vuot qua kich thuoc cho phep.");

        try
        {
            using var document = JsonDocument.Parse(metadataJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException("Metadata chu ky phai la mot JSON object.");

            var hasSignatureImage = TryGetProperty(document.RootElement, "SignatureImageDataUrl", out var image);
            if (hasSignatureImage)
            {
                ValidatePngDataUrl(image.GetString());
            }
            if (TryGetProperty(document.RootElement, "SignatureCaptured", out var captured) &&
                captured.ValueKind == JsonValueKind.True &&
                !hasSignatureImage)
            {
                throw new InvalidOperationException("Metadata chu ky thieu anh PNG da chup.");
            }

            return metadataJson;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Metadata chu ky khong phai JSON hop le.", ex);
        }
    }

    private static void ValidatePngDataUrl(string? dataUrl)
    {
        if (string.IsNullOrWhiteSpace(dataUrl) ||
            !dataUrl.StartsWith(PngDataUrlPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Anh chu ky phai la PNG data URL.");
        }

        var base64 = dataUrl[PngDataUrlPrefix.Length..];
        var maxBase64Chars = ((MaxSignatureImageBytes + 2) / 3) * 4;
        if (base64.Length == 0 || base64.Length > maxBase64Chars)
            throw new InvalidOperationException("Anh chu ky vuot qua kich thuoc cho phep.");

        byte[] imageBytes;
        try
        {
            imageBytes = Convert.FromBase64String(base64);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("Anh chu ky PNG khong hop le.", ex);
        }

        if (imageBytes.Length > MaxSignatureImageBytes ||
            imageBytes.Length < PngHeader.Length ||
            !imageBytes.AsSpan(0, PngHeader.Length).SequenceEqual(PngHeader))
        {
            throw new InvalidOperationException("Anh chu ky PNG khong hop le.");
        }
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
