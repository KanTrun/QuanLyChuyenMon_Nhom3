using Microsoft.AspNetCore.DataProtection;
using TelemedicineLandingPage.Services.Admin;

namespace TelemedicineLandingPage.Tests.Admin.Sql;

public sealed class ProcedureAttachmentAccessTokenServiceTests
{
    [Fact]
    public void CreateToken_ValidatesForSameAttachmentWithinLifetime()
    {
        var service = new ProcedureAttachmentAccessTokenService(new EphemeralDataProtectionProvider());
        var attachmentId = Guid.NewGuid();
        var issuedAt = new DateTime(2026, 6, 24, 8, 0, 0, DateTimeKind.Utc);
        var token = service.CreateToken(attachmentId, issuedAt);

        Assert.True(service.TryValidateToken(attachmentId, token, issuedAt.AddHours(1)));
        Assert.False(service.TryValidateToken(Guid.NewGuid(), token, issuedAt.AddHours(1)));
    }

    [Fact]
    public void CreateToken_RejectsExpiredToken()
    {
        var service = new ProcedureAttachmentAccessTokenService(new EphemeralDataProtectionProvider());
        var attachmentId = Guid.NewGuid();
        var issuedAt = new DateTime(2026, 6, 24, 8, 0, 0, DateTimeKind.Utc);
        var token = service.CreateToken(attachmentId, issuedAt);

        Assert.False(service.TryValidateToken(attachmentId, token, issuedAt.AddHours(25)));
    }

    [Fact]
    public void CreateDownloadUrl_IncludesAccessTokenQuery()
    {
        var service = new ProcedureAttachmentAccessTokenService(new EphemeralDataProtectionProvider());
        var attachmentId = Guid.NewGuid();
        var issuedAt = new DateTime(2026, 6, 24, 8, 0, 0, DateTimeKind.Utc);

        var url = service.CreateDownloadUrl("https://localhost:8080/", attachmentId, issuedAt);

        Assert.StartsWith($"https://localhost:8080/api/procedure-attachments/{attachmentId:D}?accessToken=", url, StringComparison.Ordinal);
        var token = Uri.UnescapeDataString(url[(url.IndexOf("accessToken=", StringComparison.Ordinal) + "accessToken=".Length)..]);
        Assert.True(service.TryValidateToken(attachmentId, token, issuedAt));
    }
}
