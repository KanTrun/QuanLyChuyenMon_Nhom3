using System.Text.Json.Serialization;

namespace TelemedicineLandingPage.Application.Signature;

public sealed record SmartCaSignatureDocument(
    string DocumentId,
    string Name,
    string Hash,
    string FileType = "json",
    string SignType = "hash");

public sealed record SmartCaStartRequest(
    string UserId,
    string? SerialNumber,
    string TransactionId,
    string TransactionDescription,
    SmartCaSignatureDocument Document);

public sealed record SmartCaStartResult(
    string TransactionId,
    string TransactionCode,
    string Message);

public sealed record SmartCaStatusResult(
    SmartCaExternalStatus Status,
    string? TransactionId,
    string? Message,
    IReadOnlyList<SmartCaSignedDocument> SignedDocuments);

public sealed record SmartCaSignedDocument(
    string DocumentId,
    string? SignatureValue,
    string? TimestampSignature);

public enum SmartCaExternalStatus
{
    Waiting,
    Signed,
    Rejected,
    Expired,
    Failed,
    Unknown
}

internal sealed record SmartCaEnvelope<T>(
    [property: JsonPropertyName("status_code")] int? StatusCode,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("data")] T? Data);

internal sealed record SmartCaSignPayload(
    [property: JsonPropertyName("sp_id")] string SpId,
    [property: JsonPropertyName("sp_password")] string SpPassword,
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("transaction_id")] string TransactionId,
    [property: JsonPropertyName("transaction_desc")] string TransactionDescription,
    [property: JsonPropertyName("serial_number")] string? SerialNumber,
    [property: JsonPropertyName("time_stamp")] string TimeStamp,
    [property: JsonPropertyName("sign_files")] IReadOnlyList<SmartCaSignFilePayload> SignFiles);

internal sealed record SmartCaSignFilePayload(
    [property: JsonPropertyName("data_to_be_signed")] string DataToBeSigned,
    [property: JsonPropertyName("doc_id")] string DocumentId,
    [property: JsonPropertyName("file_type")] string FileType,
    [property: JsonPropertyName("sign_type")] string SignType);

internal sealed record SmartCaSignResponseData(
    [property: JsonPropertyName("transaction_id")] string? TransactionId,
    [property: JsonPropertyName("tran_code")] string? TransactionCode);

internal sealed record SmartCaStatusResponseData(
    [property: JsonPropertyName("transaction_id")] string? TransactionId,
    [property: JsonPropertyName("signatures")] IReadOnlyList<SmartCaStatusSignature>? Signatures);

internal sealed record SmartCaStatusSignature(
    [property: JsonPropertyName("doc_id")] string? DocumentId,
    [property: JsonPropertyName("signature_value")] string? SignatureValue,
    [property: JsonPropertyName("timestamp_signature")] string? TimestampSignature);

internal sealed record SmartCaCertificatePayload(
    [property: JsonPropertyName("sp_id")] string SpId,
    [property: JsonPropertyName("sp_password")] string SpPassword,
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("serial_number")] string? SerialNumber,
    [property: JsonPropertyName("transaction_id")] string TransactionId);

internal sealed record SmartCaCertificateResponseData(
    [property: JsonPropertyName("user_certificates")] IReadOnlyList<SmartCaCertificateData>? UserCertificates);

internal sealed record SmartCaCertificateData(
    [property: JsonPropertyName("serial_number")] string? SerialNumber,
    [property: JsonPropertyName("cert_subject")] string? Subject,
    [property: JsonPropertyName("cert_valid_to")] DateTime? ValidTo);

internal sealed record SmartCaOAuthTokenResponse(
    [property: JsonPropertyName("access_token")] string? AccessToken,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken,
    [property: JsonPropertyName("token_type")] string? TokenType,
    [property: JsonPropertyName("expires_in")] int? ExpiresIn,
    [property: JsonPropertyName("scope")] string? Scope);

internal sealed record SmartCaOAuthEnvelope<T>(
    [property: JsonPropertyName("code")] int? Code,
    [property: JsonPropertyName("codeDesc")] string? CodeDescription,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("content")] T? Content);

internal sealed record SmartCaOAuthCredentialInfoRequest(
    [property: JsonPropertyName("credentialId")] string CredentialId,
    [property: JsonPropertyName("certificates")] string Certificates,
    [property: JsonPropertyName("certInfo")] bool CertInfo,
    [property: JsonPropertyName("authInfo")] bool AuthInfo);

internal sealed record SmartCaOAuthCredentialInfo(
    [property: JsonPropertyName("cert")] SmartCaOAuthCertificate? Certificate,
    [property: JsonPropertyName("status")] string? Status);

internal sealed record SmartCaOAuthCertificate(
    [property: JsonPropertyName("serialNumber")] string? SerialNumber,
    [property: JsonPropertyName("subjectDN")] string? SubjectDn,
    [property: JsonPropertyName("validTo")] string? ValidTo);

internal sealed record SmartCaOAuthSignHashPayload(
    [property: JsonPropertyName("credentialId")] string CredentialId,
    [property: JsonPropertyName("refTranId")] string RefTranId,
    [property: JsonPropertyName("notifyUrl")] string? NotifyUrl,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("datas")] IReadOnlyList<SmartCaOAuthHashData> Datas);

internal sealed record SmartCaOAuthHashData(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("hash")] string Hash);

internal sealed record SmartCaOAuthTransactionResponse(
    [property: JsonPropertyName("tranId")] string? TransactionId);

internal sealed record SmartCaOAuthTransactionInfoRequest(
    [property: JsonPropertyName("tranId")] string TransactionId);

internal sealed record SmartCaOAuthTransactionInfo(
    [property: JsonPropertyName("refTranId")] string? RefTranId,
    [property: JsonPropertyName("documents")] IReadOnlyList<SmartCaOAuthSignedDocument>? Documents,
    [property: JsonPropertyName("tranId")] string? TransactionId,
    [property: JsonPropertyName("credentialId")] string? CredentialId,
    [property: JsonPropertyName("tranStatus")] int? TransactionStatus,
    [property: JsonPropertyName("tranStatusDesc")] string? TransactionStatusDescription);

internal sealed record SmartCaOAuthSignedDocument(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("hash")] string? Hash,
    [property: JsonPropertyName("sig")] string? Signature,
    [property: JsonPropertyName("dataSigned")] string? DataSigned);
