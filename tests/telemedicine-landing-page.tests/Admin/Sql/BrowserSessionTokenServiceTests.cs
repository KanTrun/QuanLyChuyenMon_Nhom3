using Microsoft.AspNetCore.DataProtection;
using TelemedicineLandingPage.Services.Auth;

namespace TelemedicineLandingPage.Tests.Admin.Sql;

public sealed class BrowserSessionTokenServiceTests
{
    [Fact]
    public void IssuedToken_ValidatesSameUser()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 6, 3, 8, 0, 0, TimeSpan.Zero));
        var service = new BrowserSessionTokenService(new EphemeralDataProtectionProvider(), time);
        var userId = Guid.NewGuid();

        var token = service.IssueToken(userId);

        Assert.True(service.TryValidateToken(token, out var restoredUserId));
        Assert.Equal(userId, restoredUserId);
    }

    [Fact]
    public void TamperedToken_IsRejected()
    {
        var service = new BrowserSessionTokenService(new EphemeralDataProtectionProvider());
        var token = service.IssueToken(Guid.NewGuid());

        Assert.False(service.TryValidateToken(token + "x", out var restoredUserId));
        Assert.Equal(Guid.Empty, restoredUserId);
    }

    [Fact]
    public void ExpiredToken_IsRejected()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 6, 3, 8, 0, 0, TimeSpan.Zero));
        var service = new BrowserSessionTokenService(new EphemeralDataProtectionProvider(), time);
        var token = service.IssueToken(Guid.NewGuid());

        time.Advance(TimeSpan.FromHours(8).Add(TimeSpan.FromSeconds(1)));

        Assert.False(service.TryValidateToken(token, out _));
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan offset) => _now = _now.Add(offset);
    }
}
