using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Application.Signature;

public interface ISignatureService
{
    Task<(SignatureResult Result, SignatureRecord? Record)> CreateInternalSignatureAsync(
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

    Task<SignatureResult> RevokeInternalSignatureAsync(
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
    InvalidState
}
