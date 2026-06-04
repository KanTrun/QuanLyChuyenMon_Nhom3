using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Application.Signature;

public interface ISignatureService
{
    Task<(SignatureResult Result, SignatureRecord? Record)> CreateDemoSignatureAsync(
        string targetType,
        Guid targetId,
        Guid signerUserId,
        string signerUsername,
        string? metadataJson = null,
        CancellationToken cancellationToken = default);

    Task<SignatureRecord?> GetSignatureAsync(
        string targetType,
        Guid targetId,
        CancellationToken cancellationToken = default);

    Task<SignatureTransactionRecord?> GetLatestSmartCaTransactionAsync(
        string targetType,
        Guid targetId,
        Guid? signerUserId = null,
        CancellationToken cancellationToken = default);

    SmartCaReadiness GetSmartCaReadiness();

    Task<(SignatureResult Result, SignatureTransactionRecord? Transaction)> StartSmartCaSignatureAsync(
        string targetType,
        Guid targetId,
        Guid signerUserId,
        string signerUsername,
        string? metadataJson = null,
        CancellationToken cancellationToken = default);

    Task<(SignatureResult Result, SignatureRecord? Record, SignatureTransactionRecord? Transaction)> RefreshSmartCaSignatureAsync(
        Guid signatureTransactionId,
        Guid actorUserId,
        string actorUsername,
        CancellationToken cancellationToken = default);

    Task<SignatureResult> RevokeDemoSignatureAsync(
        string targetType,
        Guid targetId,
        Guid actorUserId,
        string actorUsername,
        string reason,
        CancellationToken cancellationToken = default);

    bool VerifyIntegrity(SignatureRecord record);
}

public enum SignatureResult
{
    Created,
    AlreadySigned,
    TargetNotFound,
    Unauthorized,
    Revoked,
    InvalidState,
    ProviderNotConfigured,
    PendingExternalConfirmation,
    ExternalProviderRejected,
    ExternalProviderExpired,
    ExternalProviderFailed
}
