using System.Text;
using Microsoft.AspNetCore.DataProtection;

namespace TelemedicineLandingPage.Services.Admin;

public interface IProcedureAttachmentAccessTokenService
{
    string CreateToken(Guid attachmentId, DateTime issuedAtUtc);
    bool TryValidateToken(Guid attachmentId, string? token, DateTime nowUtc);
    string CreateDownloadUrl(string baseUrl, Guid attachmentId, DateTime issuedAtUtc);
}

public sealed class ProcedureAttachmentAccessTokenService : IProcedureAttachmentAccessTokenService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);
    private readonly IDataProtector _protector;

    public ProcedureAttachmentAccessTokenService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("QLCM.ProcedureAttachment.Download.v1");
    }

    public string CreateToken(Guid attachmentId, DateTime issuedAtUtc)
    {
        var payload = $"{attachmentId:N}|{issuedAtUtc.ToUniversalTime().Ticks}";
        return _protector.Protect(Convert.ToBase64String(Encoding.UTF8.GetBytes(payload)));
    }

    public bool TryValidateToken(Guid attachmentId, string? token, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;
        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(_protector.Unprotect(token)));
            var parts = decoded.Split('|', 2);
            if (parts.Length != 2 || !Guid.TryParse(parts[0], out var tokenAttachmentId)) return false;
            if (tokenAttachmentId != attachmentId) return false;
            if (!long.TryParse(parts[1], out var ticks)) return false;
            var issuedAt = new DateTime(ticks, DateTimeKind.Utc);
            return nowUtc.ToUniversalTime() - issuedAt <= TokenLifetime;
        }
        catch
        {
            return false;
        }
    }

    public string CreateDownloadUrl(string baseUrl, Guid attachmentId, DateTime issuedAtUtc)
    {
        var normalizedBase = baseUrl.TrimEnd('/');
        var token = Uri.EscapeDataString(CreateToken(attachmentId, issuedAtUtc));
        return $"{normalizedBase}/api/procedure-attachments/{attachmentId:D}?accessToken={token}";
    }
}
