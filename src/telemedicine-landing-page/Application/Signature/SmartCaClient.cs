using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace TelemedicineLandingPage.Application.Signature;

public sealed class SmartCaClient : ISmartCaClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly SmartCaOptions _options;

    public SmartCaClient(HttpClient http, IOptions<SmartCaOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<SmartCaStartResult> StartHashSignatureAsync(
        SmartCaStartRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureReady();
        var payload = new SmartCaSignPayload(
            _options.SpId.Trim(),
            _options.SpPassword,
            request.UserId.Trim(),
            request.TransactionId,
            request.TransactionDescription,
            NullIfBlank(request.SerialNumber),
            DateTime.UtcNow.ToString("yyyyMMddHHmmssZ"),
            [
                new SmartCaSignFilePayload(
                    request.Document.Hash,
                    request.Document.DocumentId,
                    request.Document.FileType,
                    request.Document.SignType)
            ]);

        var response = await PostAsync<SmartCaSignPayload, SmartCaSignResponseData>(
            "v1/signatures/sign",
            payload,
            cancellationToken);

        var transactionId = response.Data?.TransactionId;
        var transactionCode = response.Data?.TransactionCode;
        if (string.IsNullOrWhiteSpace(transactionId) || string.IsNullOrWhiteSpace(transactionCode))
            throw new SmartCaClientException("SmartCA không trả mã giao dịch hợp lệ.");

        return new SmartCaStartResult(transactionId, transactionCode, response.Message ?? "Chờ người dùng xác nhận");
    }

    public async Task<SmartCaStatusResult> GetSignatureStatusAsync(
        string transactionCode,
        CancellationToken cancellationToken = default)
    {
        EnsureReady();
        if (string.IsNullOrWhiteSpace(transactionCode))
            throw new SmartCaClientException("Thiếu mã giao dịch SmartCA.");

        var response = await PostAsync<object, SmartCaStatusResponseData>(
            $"v1/signatures/sign/{Uri.EscapeDataString(transactionCode)}/status",
            new { },
            cancellationToken);

        var documents = response.Data?.Signatures?
            .Where(s => !string.IsNullOrWhiteSpace(s.DocumentId))
            .Select(s => new SmartCaSignedDocument(
                s.DocumentId!,
                NullIfBlank(s.SignatureValue),
                NullIfBlank(s.TimestampSignature)))
            .ToList() ?? [];

        return new SmartCaStatusResult(
            MapStatus(response.StatusCode, response.Message, documents),
            response.Data?.TransactionId,
            response.Message,
            documents);
    }

    public async Task<(string? Subject, string? Serial, DateTime? Expiry)> GetCertificateAsync(
        string subscriberId,
        string? serialNumber,
        string transactionId,
        CancellationToken cancellationToken = default)
    {
        EnsureReady();
        if (string.IsNullOrWhiteSpace(subscriberId))
            throw new SmartCaClientException("Thiếu thuê bao SmartCA.");
        if (string.IsNullOrWhiteSpace(transactionId))
            throw new SmartCaClientException("Thiếu giao dịch SmartCA.");

        var payload = new SmartCaCertificatePayload(
            _options.SpId.Trim(),
            _options.SpPassword,
            subscriberId.Trim(),
            NullIfBlank(serialNumber),
            transactionId);

        var response = await PostAsync<SmartCaCertificatePayload, SmartCaCertificateResponseData>(
            "v1/credentials/get_certificate",
            payload,
            cancellationToken);
        var certificate = response.Data?.UserCertificates?.FirstOrDefault();
        return (certificate?.Subject, certificate?.SerialNumber, certificate?.ValidTo);
    }

    private async Task<SmartCaEnvelope<TResponse>> PostAsync<TRequest, TResponse>(
        string relativePath,
        TRequest payload,
        CancellationToken cancellationToken)
    {
        using var response = await _http.PostAsJsonAsync(Endpoint(relativePath), payload, JsonOptions, cancellationToken);
        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new SmartCaClientException($"SmartCA trả HTTP {(int)response.StatusCode}.");

        SmartCaEnvelope<TResponse>? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<SmartCaEnvelope<TResponse>>(text, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new SmartCaClientException("SmartCA trả dữ liệu không đúng định dạng JSON.", ex);
        }

        if (envelope is null)
            throw new SmartCaClientException("SmartCA trả phản hồi rỗng.");
        if (envelope.StatusCode is not null and not 200)
            throw new SmartCaClientException($"SmartCA lỗi {envelope.StatusCode}: {SafeMessage(envelope.Message)}");

        return envelope;
    }

    private string Endpoint(string relativePath)
    {
        var prefix = _options.ApiPrefix.Trim().Trim('/');
        var path = relativePath.Trim().Trim('/');
        return string.IsNullOrWhiteSpace(prefix) ? path : $"{prefix}/{path}";
    }

    private void EnsureReady()
    {
        if (!_options.IsReady)
            throw new SmartCaClientException("SmartCA sandbox chưa được cấu hình đầy đủ.");
    }

    private static SmartCaExternalStatus MapStatus(
        int? statusCode,
        string? message,
        IReadOnlyList<SmartCaSignedDocument> documents)
    {
        if (documents.Any(d => !string.IsNullOrWhiteSpace(d.SignatureValue)))
            return SmartCaExternalStatus.Signed;

        var normalized = (message ?? string.Empty).Trim().ToUpperInvariant();
        if (normalized.Contains("WAIT") || normalized.Contains("PENDING") || normalized.Contains("CONFIRM"))
            return SmartCaExternalStatus.Waiting;
        if (normalized.Contains("REJECT"))
            return SmartCaExternalStatus.Rejected;
        if (normalized.Contains("EXPIRED"))
            return SmartCaExternalStatus.Expired;
        if (normalized.Contains("FAILED") || normalized.Contains("ERROR"))
            return SmartCaExternalStatus.Failed;
        if (statusCode is 200)
            return SmartCaExternalStatus.Waiting;

        return SmartCaExternalStatus.Unknown;
    }

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string SafeMessage(string? message)
        => string.IsNullOrWhiteSpace(message) ? "Không có mô tả" : message.Trim();
}
