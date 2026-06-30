using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace TelemedicineLandingPage.Services.Auth;

/// <summary>Issues and validates protected, expiring browser session tokens.</summary>
public sealed class BrowserSessionTokenService
{
    private const string ProtectorPurpose = "qlcm.browser-session.v1";
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);

    private readonly IDataProtector _protector;
    private readonly TimeProvider _timeProvider;

    public BrowserSessionTokenService(IDataProtectionProvider dataProtectionProvider)
        : this(dataProtectionProvider, TimeProvider.System)
    {
    }

    public BrowserSessionTokenService(IDataProtectionProvider dataProtectionProvider, TimeProvider timeProvider)
    {
        _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
        _timeProvider = timeProvider;
    }

    public string IssueToken(Guid userId)
        => IssueToken(userId, Guid.Empty);

    public string IssueToken(Guid userId, Guid sessionId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);

        var payload = new BrowserSessionPayload(
            userId,
            sessionId,
            _timeProvider.GetUtcNow().Add(SessionLifetime));
        return _protector.Protect(JsonSerializer.Serialize(payload));
    }

    public bool TryValidateToken(string? token, out Guid userId)
    {
        var isValid = TryReadToken(token, out var identity) == BrowserSessionTokenStatus.Valid;
        userId = identity.UserId;
        return isValid;
    }

    public bool TryValidateToken(string? token, out BrowserSessionIdentity identity)
    {
        var status = TryReadToken(token, out identity);
        return status == BrowserSessionTokenStatus.Valid;
    }

    public BrowserSessionTokenStatus TryReadToken(string? token, out BrowserSessionIdentity identity)
    {
        identity = BrowserSessionIdentity.Empty;
        if (string.IsNullOrWhiteSpace(token))
        {
            return BrowserSessionTokenStatus.Missing;
        }

        try
        {
            var json = _protector.Unprotect(token);
            var payload = JsonSerializer.Deserialize<BrowserSessionPayload>(json);
            if (payload is null ||
                payload.UserId == Guid.Empty ||
                payload.ExpiresAtUtc <= _timeProvider.GetUtcNow())
            {
                return payload is not null && payload.ExpiresAtUtc <= _timeProvider.GetUtcNow()
                    ? BrowserSessionTokenStatus.Expired
                    : BrowserSessionTokenStatus.Invalid;
            }

            identity = new BrowserSessionIdentity(payload.UserId, payload.SessionId, payload.ExpiresAtUtc);
            return BrowserSessionTokenStatus.Valid;
        }
        catch (CryptographicException)
        {
            return BrowserSessionTokenStatus.Invalid;
        }
        catch (JsonException)
        {
            return BrowserSessionTokenStatus.Invalid;
        }
    }

    public sealed record BrowserSessionIdentity(Guid UserId, Guid SessionId, DateTimeOffset ExpiresAtUtc)
    {
        public static BrowserSessionIdentity Empty { get; } = new(Guid.Empty, Guid.Empty, DateTimeOffset.MinValue);
    }

    public enum BrowserSessionTokenStatus
    {
        Missing,
        Valid,
        Expired,
        Invalid
    }

    private sealed record BrowserSessionPayload(Guid UserId, Guid SessionId, DateTimeOffset ExpiresAtUtc);
}
