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
    private const string DemoProviderCode = "demo";
    private const string SignPermission = "SCR_CLINICAL:SIGN_PROTOCOL_APPLICATION";
    private const string RevokePermission = "SCR_ADMIN:MANAGE_SIGNATURES";
    private static readonly string[] SignPermissionAliases =
    [
        SignPermission,
        "SCR_CLINICAL:EXECUTE",
        "PERM_CLINICAL_execute"
    ];

    private readonly MedDbContext _db;
    private readonly EffectivePermissionResolver _permissions;
    private readonly IWorkflowGuard<PatientProtocolApplication, string> _workflow;

    public SignatureService(
        MedDbContext db,
        EffectivePermissionResolver permissions,
        IWorkflowGuard<PatientProtocolApplication, string> workflow)
    {
        _db = db;
        _permissions = permissions;
        _workflow = workflow;
    }

    public async Task<(SignatureResult Result, SignatureRecord? Record)> CreateDemoSignatureAsync(
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

        var existing = await GetSignatureAsync(targetType, targetId, cancellationToken);
        if (existing is not null)
            return (SignatureResult.AlreadySigned, existing);

        var target = await _db.PatientProtocolApplications
            .FirstOrDefaultAsync(a => a.PatientProtocolApplicationId == targetId, cancellationToken);
        if (target is null)
            return (SignatureResult.TargetNotFound, null);

        if (!_workflow.CanTransition(target.ApplicationStatus, "signed"))
            return (SignatureResult.InvalidState, null);

        var signedAt = DateTime.UtcNow;
        var correlationId = Guid.NewGuid();
        var record = new SignatureRecord
        {
            TargetType = targetType,
            TargetId = targetId,
            SignerUserId = signerUserId,
            SignerUsername = signerUsername,
            ProviderCode = DemoProviderCode,
            IsLegallyValid = false,
            SignedAt = signedAt,
            SignatureHash = ComputeHash(targetType, targetId, signerUserId, signedAt, DemoProviderCode),
            MetadataJson = metadataJson,
            CorrelationId = correlationId
        };

        _db.SignatureRecords.Add(record);
        _db.PatientProtocolApplications.Entry(target).CurrentValues.SetValues(target with
        {
            ApplicationStatus = "signed",
            AppliedAt = target.AppliedAt ?? signedAt
        });
        _db.AuditLogs.Add(new AuditLog
        {
            CorrelationId = correlationId,
            ActorUserId = signerUserId,
            ActorUsername = signerUsername,
            ActionCode = "sign",
            TargetType = targetType,
            TargetId = targetId.ToString(),
            MetadataJson = JsonSerializer.Serialize(new
            {
                ProviderCode = DemoProviderCode,
                IsLegallyValid = false
            })
        });

        await _db.SaveChangesAsync(cancellationToken);
        return (SignatureResult.Created, record);
    }

    public Task<SignatureRecord?> GetSignatureAsync(
        string targetType,
        Guid targetId,
        CancellationToken cancellationToken = default)
        => _db.SignatureRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TargetType == targetType && s.TargetId == targetId, cancellationToken);

    public async Task<SignatureResult> RevokeDemoSignatureAsync(
        string targetType,
        Guid targetId,
        Guid actorUserId,
        string actorUsername,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Lý do thu hồi chữ ký là bắt buộc.");
        if (!IsSupportedTarget(targetType))
            return SignatureResult.TargetNotFound;
        if (!CanRevoke(actorUserId, actorUsername))
            return SignatureResult.Unauthorized;

        var target = await _db.PatientProtocolApplications
            .FirstOrDefaultAsync(a => a.PatientProtocolApplicationId == targetId, cancellationToken);
        if (target is null)
            return SignatureResult.TargetNotFound;
        if (!_workflow.CanTransition(target.ApplicationStatus, "revoked"))
            return SignatureResult.InvalidState;

        var correlationId = Guid.NewGuid();
        _db.PatientProtocolApplications.Entry(target).CurrentValues.SetValues(target with
        {
            ApplicationStatus = "revoked"
        });
        _db.AuditLogs.Add(new AuditLog
        {
            CorrelationId = correlationId,
            ActorUserId = actorUserId,
            ActorUsername = actorUsername,
            ActionCode = "revoke",
            TargetType = targetType,
            TargetId = targetId.ToString(),
            MetadataJson = JsonSerializer.Serialize(new { Reason = reason.Trim() })
        });

        await _db.SaveChangesAsync(cancellationToken);
        return SignatureResult.Revoked;
    }

    public bool VerifyIntegrity(SignatureRecord record)
        => string.Equals(
            record.SignatureHash,
            ComputeHash(record.TargetType, record.TargetId, record.SignerUserId, record.SignedAt, record.ProviderCode),
            StringComparison.OrdinalIgnoreCase);

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
        string providerCode)
    {
        var payload = $"{targetType}:{targetId}:{signerUserId}:{signedAt:O}:{providerCode}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }
}
