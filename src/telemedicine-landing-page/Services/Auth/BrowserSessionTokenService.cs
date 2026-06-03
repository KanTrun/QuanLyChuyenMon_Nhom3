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
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);

        var payload = new BrowserSessionPayload(
            userId,
            _timeProvider.GetUtcNow().Add(SessionLifetime));
        return _protector.Protect(JsonSerializer.Serialize(payload));
    }

    public bool TryValidateToken(string? token, out Guid userId)
    {
        userId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var json = _protector.Unprotect(token);
            var payload = JsonSerializer.Deserialize<BrowserSessionPayload>(json);
            if (payload is null ||
                payload.UserId == Guid.Empty ||
                payload.ExpiresAtUtc <= _timeProvider.GetUtcNow())
            {
                return false;
            }

            userId = payload.UserId;
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private sealed record BrowserSessionPayload(Guid UserId, DateTimeOffset ExpiresAtUtc);
}
