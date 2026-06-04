namespace TelemedicineLandingPage.Application.Signature;

public interface ISmartCaClient
{
    Task<SmartCaStartResult> StartHashSignatureAsync(
        SmartCaStartRequest request,
        CancellationToken cancellationToken = default);

    Task<SmartCaStatusResult> GetSignatureStatusAsync(
        string transactionCode,
        CancellationToken cancellationToken = default);

    Task<(string? Subject, string? Serial, DateTime? Expiry)> GetCertificateAsync(
        string subscriberId,
        string? serialNumber,
        string transactionId,
        CancellationToken cancellationToken = default);
}

public sealed class SmartCaClientException : Exception
{
    public SmartCaClientException(string message) : base(message) { }
    public SmartCaClientException(string message, Exception innerException) : base(message, innerException) { }
}
