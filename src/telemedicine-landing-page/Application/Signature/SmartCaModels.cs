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
