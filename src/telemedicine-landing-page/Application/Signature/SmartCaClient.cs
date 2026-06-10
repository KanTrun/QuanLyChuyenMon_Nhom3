using System.Globalization;
using System.Net.Http.Headers;
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
        => _options.IsOAuthMode
            ? await StartOAuthHashSignatureAsync(request, cancellationToken)
            : await StartDirectHashSignatureAsync(request, cancellationToken);

    public async Task<SmartCaStatusResult> GetSignatureStatusAsync(
        string transactionCode,
        CancellationToken cancellationToken = default)
        => _options.IsOAuthMode
            ? await GetOAuthSignatureStatusAsync(transactionCode, cancellationToken)
            : await GetDirectSignatureStatusAsync(transactionCode, cancellationToken);

    public async Task<(string? Subject, string? Serial, DateTime? Expiry)> GetCertificateAsync(
        string subscriberId,
        string? serialNumber,
        string transactionId,
        CancellationToken cancellationToken = default)
        => _options.IsOAuthMode
            ? await GetOAuthCertificateAsync(serialNumber, cancellationToken)
            : await GetDirectCertificateAsync(subscriberId, serialNumber, transactionId, cancellationToken);

    private async Task<SmartCaStartResult> StartDirectHashSignatureAsync(
        SmartCaStartRequest request,
        CancellationToken cancellationToken)
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

        var response = await DirectPostAsync<SmartCaSignPayload, SmartCaSignResponseData>(
            "v1/signatures/sign",
            payload,
            cancellationToken);

        var transactionId = response.Data?.TransactionId;
        var transactionCode = response.Data?.TransactionCode;
        if (string.IsNullOrWhiteSpace(transactionId) || string.IsNullOrWhiteSpace(transactionCode))
            throw new SmartCaClientException("SmartCA did not return a valid transaction code.");

        return new SmartCaStartResult(transactionId, transactionCode, response.Message ?? "Waiting for confirmation");
    }

    private async Task<SmartCaStatusResult> GetDirectSignatureStatusAsync(
        string transactionCode,
        CancellationToken cancellationToken)
    {
        EnsureReady();
        if (string.IsNullOrWhiteSpace(transactionCode))
            throw new SmartCaClientException("Missing SmartCA transaction code.");

        var response = await DirectPostAsync<object, SmartCaStatusResponseData>(
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
            MapDirectStatus(response.StatusCode, response.Message, documents),
            response.Data?.TransactionId,
            response.Message,
            documents);
    }

    private async Task<(string? Subject, string? Serial, DateTime? Expiry)> GetDirectCertificateAsync(
        string subscriberId,
        string? serialNumber,
        string transactionId,
        CancellationToken cancellationToken)
    {
        EnsureReady();
        if (string.IsNullOrWhiteSpace(subscriberId))
            throw new SmartCaClientException("Missing SmartCA subscriber.");
        if (string.IsNullOrWhiteSpace(transactionId))
            throw new SmartCaClientException("Missing SmartCA transaction.");

        var payload = new SmartCaCertificatePayload(
            _options.SpId.Trim(),
            _options.SpPassword,
            subscriberId.Trim(),
            NullIfBlank(serialNumber),
            transactionId);

        var response = await DirectPostAsync<SmartCaCertificatePayload, SmartCaCertificateResponseData>(
            "v1/credentials/get_certificate",
            payload,
            cancellationToken);
        var certificate = response.Data?.UserCertificates?.FirstOrDefault();
        return (certificate?.Subject, certificate?.SerialNumber, certificate?.ValidTo);
    }

    private async Task<SmartCaStartResult> StartOAuthHashSignatureAsync(
        SmartCaStartRequest request,
        CancellationToken cancellationToken)
    {
        EnsureReady();
        var credentialId = await ResolveOAuthCredentialIdAsync(cancellationToken);
        var payload = new SmartCaOAuthSignHashPayload(
            credentialId,
            request.TransactionId,
            NullIfBlank(_options.CallbackUrl),
            request.TransactionDescription,
            [new SmartCaOAuthHashData(request.Document.DocumentId, HexSha256ToBase64(request.Document.Hash))]);

        var response = await OAuthPostAsync<SmartCaOAuthSignHashPayload, SmartCaOAuthTransactionResponse>(
            "csc/signature/signhash",
            payload,
            cancellationToken);

        var transactionId = response.Content?.TransactionId;
        if (string.IsNullOrWhiteSpace(transactionId))
            throw new SmartCaClientException("SmartCA OAuth did not return a valid transaction id.");

        return new SmartCaStartResult(transactionId, transactionId, response.Message ?? "Waiting for confirmation");
    }

    private async Task<SmartCaStatusResult> GetOAuthSignatureStatusAsync(
        string transactionCode,
        CancellationToken cancellationToken)
    {
        EnsureReady();
        if (string.IsNullOrWhiteSpace(transactionCode))
            throw new SmartCaClientException("Missing SmartCA OAuth transaction id.");

        var response = await OAuthPostAsync<SmartCaOAuthTransactionInfoRequest, SmartCaOAuthTransactionInfo>(
            "csc/credentials/gettraninfo",
            new SmartCaOAuthTransactionInfoRequest(transactionCode.Trim()),
            cancellationToken);
        var content = response.Content;
        var documents = content?.Documents?
            .Select(d => new SmartCaSignedDocument(
                OAuthDocumentId(d, content.RefTranId),
                NullIfBlank(d.Signature ?? d.DataSigned),
                null))
            .ToList() ?? [];

        return new SmartCaStatusResult(
            MapOAuthStatus(content?.TransactionStatus, content?.TransactionStatusDescription, documents),
            content?.TransactionId,
            content?.TransactionStatusDescription ?? response.Message,
            documents);
    }

    private async Task<(string? Subject, string? Serial, DateTime? Expiry)> GetOAuthCertificateAsync(
        string? credentialId,
        CancellationToken cancellationToken)
    {
        EnsureReady();
        var resolvedCredentialId = await ResolveOAuthCredentialIdAsync(credentialId, cancellationToken);
        var info = await GetOAuthCredentialInfoAsync(resolvedCredentialId, cancellationToken);
        return (
            info.Certificate?.SubjectDn,
            info.Certificate?.SerialNumber,
            ParseSmartCaUtc(info.Certificate?.ValidTo));
    }

    private async Task<SmartCaEnvelope<TResponse>> DirectPostAsync<TRequest, TResponse>(
        string relativePath,
        TRequest payload,
        CancellationToken cancellationToken)
    {
        using var response = await _http.PostAsJsonAsync(Endpoint(relativePath), payload, JsonOptions, cancellationToken);
        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new SmartCaClientException($"SmartCA returned HTTP {(int)response.StatusCode}.");

        SmartCaEnvelope<TResponse>? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<SmartCaEnvelope<TResponse>>(text, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new SmartCaClientException("SmartCA returned invalid JSON.", ex);
        }

        if (envelope is null)
            throw new SmartCaClientException("SmartCA returned an empty response.");
        if (envelope.StatusCode is not null and not 200)
            throw new SmartCaClientException($"SmartCA error {envelope.StatusCode}: {SafeMessage(envelope.Message)}");

        return envelope;
    }

    private async Task<SmartCaOAuthEnvelope<TResponse>> OAuthPostAsync<TRequest, TResponse>(
        string relativePath,
        TRequest payload,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, relativePath.TrimStart('/'))
        {
            Content = JsonContent.Create(payload, options: JsonOptions)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetOAuthAccessTokenAsync(cancellationToken));

        using var response = await _http.SendAsync(request, cancellationToken);
        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new SmartCaClientException($"SmartCA OAuth returned HTTP {(int)response.StatusCode}.");

        SmartCaOAuthEnvelope<TResponse>? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<SmartCaOAuthEnvelope<TResponse>>(text, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new SmartCaClientException("SmartCA OAuth returned invalid JSON.", ex);
        }

        if (envelope is null)
            throw new SmartCaClientException("SmartCA OAuth returned an empty response.");
        if (envelope.Code is not null and not 0 and not 1)
            throw new SmartCaClientException($"SmartCA OAuth error {envelope.Code}: {SafeMessage(envelope.Message)}");

        return envelope;
    }

    private async Task<string> GetOAuthAccessTokenAsync(CancellationToken cancellationToken)
    {
        var body = new Dictionary<string, string>
        {
            ["client_id"] = _options.ResolvedOAuthClientId(),
            ["client_secret"] = _options.ResolvedOAuthClientSecret(),
            ["scope"] = "sign offline_access"
        };

        if (!string.IsNullOrWhiteSpace(_options.OAuthRefreshToken))
        {
            body["grant_type"] = "refresh_token";
            body["refresh_token"] = _options.OAuthRefreshToken.Trim();
        }
        else if (!string.IsNullOrWhiteSpace(_options.OAuthUsername) && !string.IsNullOrWhiteSpace(_options.OAuthPassword))
        {
            body["grant_type"] = "password";
            body["username"] = _options.OAuthUsername.Trim();
            body["password"] = _options.OAuthPassword;
        }
        else
        {
            throw new SmartCaClientException("SmartCA OAuth needs SMARTCA_OAUTH_REFRESH_TOKEN or SMARTCA_OAUTH_USERNAME/PASSWORD.");
        }

        using var response = await _http.PostAsync("auth/token", new FormUrlEncodedContent(body), cancellationToken);
        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new SmartCaClientException($"SmartCA OAuth token endpoint returned HTTP {(int)response.StatusCode}.");

        var token = JsonSerializer.Deserialize<SmartCaOAuthTokenResponse>(text, JsonOptions);
        if (string.IsNullOrWhiteSpace(token?.AccessToken))
            throw new SmartCaClientException("SmartCA OAuth token response did not contain access_token.");

        return token.AccessToken.Trim();
    }

    private async Task<string> ResolveOAuthCredentialIdAsync(CancellationToken cancellationToken)
        => await ResolveOAuthCredentialIdAsync(_options.OAuthCredentialId, cancellationToken);

    private async Task<string> ResolveOAuthCredentialIdAsync(string? preferredCredentialId, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(preferredCredentialId))
            return preferredCredentialId.Trim();

        var response = await OAuthPostAsync<object, IReadOnlyList<string>>(
            "csc/credentials/list",
            new { },
            cancellationToken);
        var credentialId = response.Content?.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        if (string.IsNullOrWhiteSpace(credentialId))
            throw new SmartCaClientException("SmartCA OAuth user has no credential to sign with.");

        return credentialId.Trim();
    }

    private async Task<SmartCaOAuthCredentialInfo> GetOAuthCredentialInfoAsync(
        string credentialId,
        CancellationToken cancellationToken)
    {
        var response = await OAuthPostAsync<SmartCaOAuthCredentialInfoRequest, SmartCaOAuthCredentialInfo>(
            "csc/credentials/info",
            new SmartCaOAuthCredentialInfoRequest(credentialId, "chain", true, true),
            cancellationToken);

        return response.Content ?? throw new SmartCaClientException("SmartCA OAuth did not return certificate info.");
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
            throw new SmartCaClientException("SmartCA sandbox is not fully configured.");
    }

    private static SmartCaExternalStatus MapDirectStatus(
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

    private static SmartCaExternalStatus MapOAuthStatus(
        int? statusCode,
        string? statusDescription,
        IReadOnlyList<SmartCaSignedDocument> documents)
    {
        if (documents.Any(d => !string.IsNullOrWhiteSpace(d.SignatureValue)))
            return SmartCaExternalStatus.Signed;

        var normalized = (statusDescription ?? string.Empty).Trim().ToUpperInvariant();
        return statusCode switch
        {
            1 => SmartCaExternalStatus.Signed,
            4000 => SmartCaExternalStatus.Waiting,
            4001 => SmartCaExternalStatus.Expired,
            4002 => SmartCaExternalStatus.Rejected,
            4003 or 4004 => SmartCaExternalStatus.Failed,
            _ when normalized.Contains("WAIT") => SmartCaExternalStatus.Waiting,
            _ when normalized.Contains("SUCCESS") => SmartCaExternalStatus.Signed,
            _ when normalized.Contains("REJECT") => SmartCaExternalStatus.Rejected,
            _ when normalized.Contains("EXPIRED") => SmartCaExternalStatus.Expired,
            _ when normalized.Contains("FAILED") => SmartCaExternalStatus.Failed,
            _ => SmartCaExternalStatus.Unknown
        };
    }

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string SafeMessage(string? message)
        => string.IsNullOrWhiteSpace(message) ? "No details" : message.Trim();

    private static string HexSha256ToBase64(string value)
    {
        if (value.Length == 64 && value.All(Uri.IsHexDigit))
            return Convert.ToBase64String(Convert.FromHexString(value));

        return value;
    }

    private static DateTime? ParseSmartCaUtc(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return DateTime.TryParseExact(
            value.Trim(),
            "yyyyMMddHHmmss'Z'",
            null,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var parsed)
            ? parsed
            : null;
    }

    private static string OAuthDocumentId(SmartCaOAuthSignedDocument document, string? fallback)
        => NullIfBlank(document.Name) ?? NullIfBlank(fallback) ?? string.Empty;
}
