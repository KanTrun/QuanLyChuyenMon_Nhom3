using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TelemedicineLandingPage.Application.Workflow;
using TelemedicineLandingPage.Data;
using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Application.Signature;

public sealed class SignatureService : ISignatureService
{
    public const string PatientProtocolApplicationTarget = "patient_protocol_application";
    public const int MaxSignatureImageBytes = 256 * 1024;
    public const int MaxMetadataJsonChars = 384 * 1024;
    private const string DemoProviderCode = "demo";
    private const string SmartCaProviderCode = SmartCaOptions.SandboxProviderCode;
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
    private readonly ISmartCaClient? _smartCaClient;
    private readonly SmartCaOptions _smartCaOptions;

    public SignatureService(
        IDbContextFactory<MedDbContext> dbFactory,
        EffectivePermissionResolver permissions,
        IWorkflowGuard<PatientProtocolApplication, string> workflow,
        ISmartCaClient? smartCaClient = null,
        IOptions<SmartCaOptions>? smartCaOptions = null)
    {
        _dbFactory = dbFactory;
        _permissions = permissions;
        _workflow = workflow;
        _smartCaClient = smartCaClient;
        _smartCaOptions = smartCaOptions?.Value ?? new SmartCaOptions();
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
            ProviderCode = DemoProviderCode,
            IsLegallyValid = false,
            SignedAt = signedAt,
            SignatureHash = ComputeHash(targetType, targetId, signerUserId, signedAt, DemoProviderCode, validatedMetadataJson),
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
                ProviderCode = DemoProviderCode,
                IsLegallyValid = false
            })
        });

        await db.SaveChangesAsync(cancellationToken);
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

    public async Task<SignatureTransactionRecord?> GetLatestSmartCaTransactionAsync(
        string targetType,
        Guid targetId,
        Guid? signerUserId = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.SignatureTransactions
            .AsNoTracking()
            .Where(t => t.TargetType == targetType &&
                        t.TargetId == targetId &&
                        t.ProviderCode == SmartCaProviderCode &&
                        (!signerUserId.HasValue || t.SignerUserId == signerUserId.Value))
            .OrderByDescending(t => t.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public SmartCaReadiness GetSmartCaReadiness()
        => new(
            _smartCaOptions.Enabled,
            _smartCaOptions.IsReady && _smartCaClient is not null,
            _smartCaOptions.BaseUrl,
            _smartCaOptions.ApiPrefix,
            string.IsNullOrWhiteSpace(_smartCaOptions.DefaultUserId) ? null : _smartCaOptions.DefaultUserId,
            _smartCaOptions.MissingFields());

    public async Task<(SignatureResult Result, SignatureTransactionRecord? Transaction)> StartSmartCaSignatureAsync(
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
        if (!_smartCaOptions.IsReady || _smartCaClient is null)
            return (SignatureResult.ProviderNotConfigured, null);
        var signerBinding = _smartCaOptions.ResolveSigner(signerUserId, signerUsername);
        if (signerBinding is null)
            return (SignatureResult.ProviderNotConfigured, null);

        var validatedMetadataJson = ValidateMetadata(metadataJson);
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var existing = await GetSignatureAsync(db, targetType, targetId, cancellationToken);
        if (existing is not null)
            return (SignatureResult.AlreadySigned, null);

        var pending = await GetLatestPendingSmartCaTransactionAsync(db, targetType, targetId, cancellationToken);
        if (pending is not null)
        {
            if (pending.SignerUserId != signerUserId)
                return (SignatureResult.Unauthorized, null);

            return (SignatureResult.PendingExternalConfirmation, pending);
        }

        var target = await db.PatientProtocolApplications
            .FirstOrDefaultAsync(a => a.PatientProtocolApplicationId == targetId, cancellationToken);
        if (target is null)
            return (SignatureResult.TargetNotFound, null);
        if (!_workflow.CanTransition(target.ApplicationStatus, "signed"))
            return (SignatureResult.InvalidState, null);

        var correlationId = Guid.NewGuid();
        var documentId = $"QLCM-{targetId:N}"[..17];
        var documentHash = ComputeSmartCaDocumentHash(target, signerUserId, validatedMetadataJson);
        var partnerTransactionId = $"QLCM-{correlationId:N}";
        SmartCaStartResult providerResult;
        try
        {
            providerResult = await _smartCaClient.StartHashSignatureAsync(
                new SmartCaStartRequest(
                    signerBinding.SubscriberId,
                    signerBinding.SerialNumber,
                    partnerTransactionId,
                    $"QLCM clinical signature {documentId}",
                    new SmartCaSignatureDocument(documentId, "qlcm-clinical-record.json", documentHash)),
                cancellationToken);
        }
        catch (SmartCaClientException ex)
        {
            var failed = BuildSmartCaTransaction(
                targetType,
                targetId,
                signerUserId,
                signerUsername,
                documentId,
                documentHash,
                "failed",
                ex.Message,
                partnerTransactionId,
                null,
                signerBinding,
                validatedMetadataJson,
                correlationId);
            db.SignatureTransactions.Add(failed);
            await db.SaveChangesAsync(cancellationToken);
            return (SignatureResult.ExternalProviderFailed, failed);
        }

        var transaction = BuildSmartCaTransaction(
            targetType,
            targetId,
            signerUserId,
            signerUsername,
            documentId,
            documentHash,
            "waiting",
            providerResult.Message,
            providerResult.TransactionId,
            providerResult.TransactionCode,
            signerBinding,
            validatedMetadataJson,
            correlationId);
        db.SignatureTransactions.Add(transaction);
        await db.SaveChangesAsync(cancellationToken);
        return (SignatureResult.PendingExternalConfirmation, transaction);
    }

    public async Task<(SignatureResult Result, SignatureRecord? Record, SignatureTransactionRecord? Transaction)> RefreshSmartCaSignatureAsync(
        Guid signatureTransactionId,
        Guid actorUserId,
        string actorUsername,
        CancellationToken cancellationToken = default)
    {
        if (!CanSign(actorUserId, actorUsername))
            return (SignatureResult.Unauthorized, null, null);
        if (!_smartCaOptions.IsReady || _smartCaClient is null)
            return (SignatureResult.ProviderNotConfigured, null, null);

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var transaction = await db.SignatureTransactions
            .FirstOrDefaultAsync(t => t.SignatureTransactionId == signatureTransactionId, cancellationToken);
        if (transaction is null)
            return (SignatureResult.TargetNotFound, null, null);

        return await RefreshSmartCaTransactionAsync(db, transaction, actorUserId, actorUsername, true, cancellationToken);
    }

    public async Task<(SignatureResult Result, SignatureRecord? Record, SignatureTransactionRecord? Transaction)> RefreshSmartCaSignatureByExternalReferenceAsync(
        string externalReference,
        CancellationToken cancellationToken = default)
    {
        if (!_smartCaOptions.IsReady || _smartCaClient is null)
            return (SignatureResult.ProviderNotConfigured, null, null);
        if (string.IsNullOrWhiteSpace(externalReference))
            return (SignatureResult.TargetNotFound, null, null);

        var reference = externalReference.Trim();
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var transaction = await db.SignatureTransactions
            .FirstOrDefaultAsync(t =>
                t.ProviderCode == SmartCaProviderCode &&
                (t.ExternalTransactionId == reference || t.ExternalTransactionCode == reference),
                cancellationToken);
        if (transaction is null)
            return (SignatureResult.TargetNotFound, null, null);

        return await RefreshSmartCaTransactionAsync(
            db,
            transaction,
            transaction.SignerUserId,
            transaction.SignerUsername ?? "smartca-callback",
            false,
            cancellationToken);
    }

    private async Task<(SignatureResult Result, SignatureRecord? Record, SignatureTransactionRecord? Transaction)> RefreshSmartCaTransactionAsync(
        MedDbContext db,
        SignatureTransactionRecord transaction,
        Guid actorUserId,
        string actorUsername,
        bool requireActorOwnership,
        CancellationToken cancellationToken)
    {
        if (requireActorOwnership && transaction.SignerUserId != actorUserId)
            return (SignatureResult.Unauthorized, null, transaction);
        if (_smartCaClient is null)
            return (SignatureResult.ProviderNotConfigured, null, transaction);

        var signerBinding = _smartCaOptions.ResolveSigner(transaction.SignerUserId, transaction.SignerUsername ?? actorUsername);
        if (signerBinding is null)
            return (SignatureResult.ProviderNotConfigured, null, transaction);
        if (!MatchesTransactionBinding(transaction, signerBinding))
            return (SignatureResult.Unauthorized, null, transaction);

        var existing = await GetSignatureAsync(db, transaction.TargetType, transaction.TargetId, cancellationToken);
        if (existing is not null)
            return (SignatureResult.AlreadySigned, existing, transaction);

        SmartCaStatusResult status;
        try
        {
            status = await _smartCaClient.GetSignatureStatusAsync(
                transaction.ExternalTransactionCode ?? string.Empty,
                cancellationToken);
        }
        catch (SmartCaClientException ex)
        {
            var failed = UpdateSmartCaTransaction(db, transaction, "failed", ex.Message, completed: false);
            await db.SaveChangesAsync(cancellationToken);
            return (SignatureResult.ExternalProviderFailed, null, failed);
        }

        if (status.Status is not SmartCaExternalStatus.Signed)
        {
            var mappedStatus = MapSmartCaStatus(status.Status);
            var updated = UpdateSmartCaTransaction(db, transaction, mappedStatus, status.Message, completed: IsTerminalSmartCaStatus(mappedStatus));
            await db.SaveChangesAsync(cancellationToken);
            return (ResultForSmartCaStatus(status.Status), null, updated);
        }

        var signedDocument = GetMatchingSignedDocument(status, transaction);
        if (signedDocument is null)
        {
            var failed = UpdateSmartCaTransaction(
                db,
                transaction,
                "failed",
                "SmartCA did not return a signature for the expected document.",
                completed: false);
            await db.SaveChangesAsync(cancellationToken);
            return (SignatureResult.ExternalProviderFailed, null, failed);
        }

        var target = await db.PatientProtocolApplications
            .FirstOrDefaultAsync(a => a.PatientProtocolApplicationId == transaction.TargetId, cancellationToken);
        if (target is null)
            return (SignatureResult.TargetNotFound, null, transaction);
        if (!_workflow.CanTransition(target.ApplicationStatus, "signed"))
            return (SignatureResult.InvalidState, null, transaction);

        (string? Subject, string? Serial, DateTime? Expiry) certificate;
        try
        {
            certificate = await GetRequiredSmartCaCertificateAsync(transaction, cancellationToken);
        }
        catch (SmartCaClientException ex)
        {
            var failed = UpdateSmartCaTransaction(db, transaction, "failed", ex.Message, completed: false);
            await db.SaveChangesAsync(cancellationToken);
            return (SignatureResult.ExternalProviderFailed, null, failed);
        }

        if (!HasRequiredCertificateEvidence(certificate) || !CertificateMatchesRequest(transaction, certificate))
        {
            var failed = UpdateSmartCaTransaction(
                db,
                transaction,
                "failed",
                "SmartCA certificate evidence is missing or does not match the requested certificate.",
                completed: false);
            await db.SaveChangesAsync(cancellationToken);
            return (SignatureResult.ExternalProviderFailed, null, failed);
        }

        var signedAt = DateTime.UtcNow;
        var finalMetadata = BuildSmartCaFinalMetadata(transaction, signedDocument, certificate);
        var record = new SignatureRecord
        {
            TargetType = transaction.TargetType,
            TargetId = transaction.TargetId,
            SignerUserId = transaction.SignerUserId,
            SignerUsername = transaction.SignerUsername,
            ProviderCode = SmartCaProviderCode,
            IsLegallyValid = true,
            SignedAt = signedAt,
            SignatureHash = ComputeHash(transaction.TargetType, transaction.TargetId, transaction.SignerUserId, signedAt, SmartCaProviderCode, finalMetadata),
            CertificateSubject = certificate.Subject,
            CertificateSerial = certificate.Serial,
            CertificateExpiry = certificate.Expiry,
            MetadataJson = finalMetadata,
            CorrelationId = transaction.CorrelationId
        };

        db.SignatureRecords.Add(record);
        db.PatientProtocolApplications.Entry(target).CurrentValues.SetValues(target with
        {
            ApplicationStatus = "signed",
            AppliedAt = target.AppliedAt ?? signedAt
        });
        UpdateSmartCaTransaction(db, transaction, "signed", status.Message ?? "SmartCA signed", completed: true, certificate);
        db.AuditLogs.Add(new AuditLog
        {
            CorrelationId = transaction.CorrelationId,
            ActorUserId = transaction.SignerUserId,
            ActorUsername = transaction.SignerUsername,
            ActionCode = "sign",
            TargetType = transaction.TargetType,
            TargetId = transaction.TargetId.ToString(),
            MetadataJson = JsonSerializer.Serialize(new
            {
                ProviderCode = SmartCaProviderCode,
                IsLegallyValid = true,
                transaction.ExternalTransactionId,
                transaction.ExternalTransactionCode,
                transaction.CaSubscriberId,
                CertificateSerial = certificate.Serial
            })
        });

        await db.SaveChangesAsync(cancellationToken);
        return (SignatureResult.Created, record, transaction);
    }

    private static Task<SignatureRecord?> GetSignatureAsync(
        MedDbContext db,
        string targetType,
        Guid targetId,
        CancellationToken cancellationToken)
        => db.SignatureRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TargetType == targetType && s.TargetId == targetId, cancellationToken);

    private static Task<SignatureTransactionRecord?> GetLatestPendingSmartCaTransactionAsync(
        MedDbContext db,
        string targetType,
        Guid targetId,
        CancellationToken cancellationToken)
        => db.SignatureTransactions
            .AsNoTracking()
            .Where(t => t.TargetType == targetType &&
                        t.TargetId == targetId &&
                        t.ProviderCode == SmartCaProviderCode &&
                        (t.TransactionStatus == "created" || t.TransactionStatus == "waiting"))
            .OrderByDescending(t => t.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);

    private static SignatureTransactionRecord BuildSmartCaTransaction(
        string targetType,
        Guid targetId,
        Guid signerUserId,
        string signerUsername,
        string documentId,
        string documentHash,
        string status,
        string? statusMessage,
        string? externalTransactionId,
        string? externalTransactionCode,
        SmartCaSignerBinding signerBinding,
        string? sourceMetadataJson,
        Guid correlationId)
    {
        var metadata = JsonSerializer.Serialize(new
        {
            Source = "qlcm_clinical_signature",
            SourceMetadata = TryReadRawJson(sourceMetadataJson),
            Provider = SmartCaProviderCode,
            CaSubscriberId = signerBinding.SubscriberId,
            RequestedCertificateSerial = signerBinding.SerialNumber
        });
        return new SignatureTransactionRecord
        {
            TargetType = targetType,
            TargetId = targetId,
            SignerUserId = signerUserId,
            SignerUsername = signerUsername,
            ProviderCode = SmartCaProviderCode,
            ExternalTransactionId = externalTransactionId,
            ExternalTransactionCode = externalTransactionCode,
            DocumentId = documentId,
            DocumentHash = documentHash,
            CaSubscriberId = signerBinding.SubscriberId,
            RequestedCertificateSerial = signerBinding.SerialNumber,
            TransactionStatus = status,
            StatusMessage = statusMessage,
            MetadataJson = metadata,
            CorrelationId = correlationId
        };
    }

    private static SignatureTransactionRecord UpdateSmartCaTransaction(
        MedDbContext db,
        SignatureTransactionRecord transaction,
        string status,
        string? statusMessage,
        bool completed,
        (string? Subject, string? Serial, DateTime? Expiry)? certificate = null)
    {
        var updated = transaction with
        {
            TransactionStatus = status,
            StatusMessage = statusMessage,
            UpdatedAt = DateTime.UtcNow,
            CompletedAt = completed ? DateTime.UtcNow : transaction.CompletedAt,
            CertificateSubject = certificate?.Subject ?? transaction.CertificateSubject,
            CertificateSerial = certificate?.Serial ?? transaction.CertificateSerial,
            CertificateExpiry = certificate?.Expiry ?? transaction.CertificateExpiry
        };
        db.SignatureTransactions.Entry(transaction).CurrentValues.SetValues(updated);
        return updated;
    }

    private async Task<(string? Subject, string? Serial, DateTime? Expiry)> GetRequiredSmartCaCertificateAsync(
        SignatureTransactionRecord transaction,
        CancellationToken cancellationToken)
    {
        if (_smartCaClient is null)
            throw new SmartCaClientException("SmartCA client is not configured.");
        if (string.IsNullOrWhiteSpace(transaction.CaSubscriberId))
            throw new SmartCaClientException("SmartCA transaction is missing subscriber binding.");

        return await _smartCaClient.GetCertificateAsync(
            transaction.CaSubscriberId,
            transaction.RequestedCertificateSerial,
            transaction.ExternalTransactionId ?? transaction.DocumentId,
            cancellationToken);
    }

    private static string BuildSmartCaFinalMetadata(
        SignatureTransactionRecord transaction,
        SmartCaSignedDocument signedDocument,
        (string? Subject, string? Serial, DateTime? Expiry) certificate)
    {
        return JsonSerializer.Serialize(new
        {
            Provider = SmartCaProviderCode,
            transaction.DocumentId,
            transaction.DocumentHash,
            transaction.CaSubscriberId,
            transaction.RequestedCertificateSerial,
            transaction.ExternalTransactionId,
            transaction.ExternalTransactionCode,
            signedDocument.SignatureValue,
            signedDocument.TimestampSignature,
            CertificateSubject = certificate.Subject,
            CertificateSerial = certificate.Serial,
            CertificateExpiry = certificate.Expiry
        });
    }

    private static SmartCaSignedDocument? GetMatchingSignedDocument(
        SmartCaStatusResult status,
        SignatureTransactionRecord transaction)
        => status.SignedDocuments.FirstOrDefault(d =>
            string.Equals(d.DocumentId, transaction.DocumentId, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(d.SignatureValue));

    private static bool MatchesTransactionBinding(SignatureTransactionRecord transaction, SmartCaSignerBinding binding)
        => string.Equals(transaction.CaSubscriberId, binding.SubscriberId, StringComparison.OrdinalIgnoreCase) &&
           (string.IsNullOrWhiteSpace(transaction.RequestedCertificateSerial) ||
            string.Equals(transaction.RequestedCertificateSerial, binding.SerialNumber, StringComparison.OrdinalIgnoreCase));

    private static bool HasRequiredCertificateEvidence((string? Subject, string? Serial, DateTime? Expiry) certificate)
        => !string.IsNullOrWhiteSpace(certificate.Subject) &&
           !string.IsNullOrWhiteSpace(certificate.Serial) &&
           certificate.Expiry.HasValue;

    private static bool CertificateMatchesRequest(
        SignatureTransactionRecord transaction,
        (string? Subject, string? Serial, DateTime? Expiry) certificate)
        => string.IsNullOrWhiteSpace(transaction.RequestedCertificateSerial) ||
           string.Equals(transaction.RequestedCertificateSerial, certificate.Serial, StringComparison.OrdinalIgnoreCase);

    private static string ComputeSmartCaDocumentHash(
        PatientProtocolApplication target,
        Guid signerUserId,
        string? metadataJson)
    {
        var canonicalPayload = JsonSerializer.Serialize(new
        {
            TargetType = PatientProtocolApplicationTarget,
            TargetId = target.PatientProtocolApplicationId,
            target.PatientRefId,
            target.EncounterRefId,
            target.ClinicalProtocolVersionId,
            target.DiagnosisCode,
            target.ApplicationStatus,
            target.AppliedAt,
            SignerUserId = signerUserId,
            Provider = SmartCaProviderCode,
            Metadata = TryReadRawJson(metadataJson)
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalPayload))).ToLowerInvariant();
    }

    private static object? TryReadRawJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            return JsonSerializer.Deserialize<JsonElement>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string MapSmartCaStatus(SmartCaExternalStatus status)
        => status switch
        {
            SmartCaExternalStatus.Signed => "signed",
            SmartCaExternalStatus.Rejected => "rejected",
            SmartCaExternalStatus.Expired => "expired",
            SmartCaExternalStatus.Failed => "failed",
            SmartCaExternalStatus.Waiting => "waiting",
            _ => "unknown"
        };

    private static bool IsTerminalSmartCaStatus(string status)
        => status is "signed" or "rejected" or "expired" or "failed" or "unknown";

    private static SignatureResult ResultForSmartCaStatus(SmartCaExternalStatus status)
        => status switch
        {
            SmartCaExternalStatus.Waiting => SignatureResult.PendingExternalConfirmation,
            SmartCaExternalStatus.Rejected => SignatureResult.ExternalProviderRejected,
            SmartCaExternalStatus.Expired => SignatureResult.ExternalProviderExpired,
            SmartCaExternalStatus.Failed => SignatureResult.ExternalProviderFailed,
            _ => SignatureResult.ExternalProviderFailed
        };

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
            MetadataJson = JsonSerializer.Serialize(new { Reason = reason.Trim() })
        });

        await db.SaveChangesAsync(cancellationToken);
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
