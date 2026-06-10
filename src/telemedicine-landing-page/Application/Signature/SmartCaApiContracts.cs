using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Application.Signature;

public sealed record SmartCaStartSignatureApiRequest(
    string TargetType,
    Guid TargetId,
    string? MetadataJson);

public sealed record SmartCaCallbackApiRequest(
    string? TransactionId,
    string? TranCode,
    string? TransactionCode,
    string? ExternalReference,
    string? Status,
    string? Message);

public sealed record SmartCaReadinessApiResponse(
    bool Enabled,
    bool Ready,
    string BaseUrl,
    string ApiPrefix,
    string CredentialMode,
    IReadOnlyList<string> MissingFields)
{
    public static SmartCaReadinessApiResponse From(SmartCaReadiness readiness)
        => new(
            readiness.Enabled,
            readiness.Ready,
            readiness.BaseUrl,
            readiness.ApiPrefix,
            readiness.CredentialMode,
            readiness.MissingFields);
}

public sealed record SmartCaOAuthAuthorizeApiResponse(
    string AuthorizationEndpoint,
    string Method,
    string ContentType,
    IReadOnlyDictionary<string, string> FormFields,
    string Note);

public sealed record SmartCaSignatureApiResponse(
    string Result,
    Guid? SignatureTransactionId,
    string? TransactionStatus,
    string? StatusMessage,
    string? ExternalTransactionId,
    string? ExternalTransactionCode,
    string? DocumentId,
    Guid? SignatureRecordId,
    bool? IsLegallyValid,
    string? CertificateSubject,
    string? CertificateSerial,
    DateTime? CertificateExpiry)
{
    public static SmartCaSignatureApiResponse From(
        SignatureResult result,
        SignatureTransactionRecord? transaction,
        SignatureRecord? record = null)
        => new(
            result.ToString(),
            transaction?.SignatureTransactionId,
            transaction?.TransactionStatus,
            transaction?.StatusMessage,
            transaction?.ExternalTransactionId,
            transaction?.ExternalTransactionCode,
            transaction?.DocumentId,
            record?.SignatureRecordId,
            record?.IsLegallyValid,
            record?.CertificateSubject ?? transaction?.CertificateSubject,
            record?.CertificateSerial ?? transaction?.CertificateSerial,
            record?.CertificateExpiry ?? transaction?.CertificateExpiry);
}
