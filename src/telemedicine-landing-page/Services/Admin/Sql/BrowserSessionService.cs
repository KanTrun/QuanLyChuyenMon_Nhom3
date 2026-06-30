using Microsoft.JSInterop;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TelemedicineLandingPage.Data;
using TelemedicineLandingPage.Hubs;
using TelemedicineLandingPage.Services.Auth;

namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>Persists a protected browser session token for Blazor circuit reloads.</summary>
public sealed class BrowserSessionService
{
    private const string SessionTokenKey = "qlcm_session";
    private const string LegacyCurrentUserKey = "qlcm_uid";

    private readonly IJSRuntime _js;
    private readonly ICurrentUserContext _userContext;
    private readonly BrowserSessionTokenService _tokens;
    private readonly MedDbContext _db;
    private readonly IHubContext<NotificationHub> _hub;

    public BrowserSessionService(
        IJSRuntime js,
        ICurrentUserContext userContext,
        BrowserSessionTokenService tokens,
        MedDbContext db,
        IHubContext<NotificationHub> hub)
    {
        _js = js;
        _userContext = userContext;
        _tokens = tokens;
        _db = db;
        _hub = hub;
    }

    public async Task PersistCurrentUserAsync()
    {
        var userId = _userContext.CurrentUser?.UserId;
        if (!userId.HasValue)
        {
            return;
        }

        var sessionId = Guid.NewGuid();
        var user = _db.Users.FirstOrDefault(item => item.UserId == userId.Value)
            ?? throw new InvalidOperationException("Người dùng không tồn tại.");
        _db.Users.Entry(user).CurrentValues.SetValues(user with
        {
            ActiveSessionId = sessionId,
            UpdatedAt = DateTime.UtcNow
        });
        _db.SaveChanges();

        await _hub.Clients.Group(NotificationHub.UserGroup(userId.Value))
            .SendAsync("SessionInvalidated", "replaced");

        await _js.InvokeVoidAsync("sessionStorage.setItem", SessionTokenKey, _tokens.IssueToken(userId.Value, sessionId));
        await ClearLegacyCurrentUserAsync();
    }

    public async Task<BrowserSessionRestoreResult> RestoreCurrentUserAsync()
    {
        await ClearLegacyCurrentUserAsync();

        if (_userContext.CurrentUser is not null)
        {
            return new BrowserSessionRestoreResult(BrowserSessionRestoreStatus.AlreadyAuthenticated);
        }

        var token = await _js.InvokeAsync<string?>("sessionStorage.getItem", SessionTokenKey);
        var validationStatus = _tokens.TryReadToken(token, out var identity);
        if (validationStatus != BrowserSessionTokenService.BrowserSessionTokenStatus.Valid)
        {
            await ClearAsync();
            return new BrowserSessionRestoreResult(validationStatus == BrowserSessionTokenService.BrowserSessionTokenStatus.Expired
                ? BrowserSessionRestoreStatus.Expired
                : BrowserSessionRestoreStatus.MissingOrInvalid);
        }

        try
        {
            _db.ChangeTracker.Clear();
            var user = _db.Users.AsNoTracking().FirstOrDefault(item => item.UserId == identity.UserId && item.DeletedAt == null);
            if (user is null)
            {
                await ClearAsync();
                return new BrowserSessionRestoreResult(BrowserSessionRestoreStatus.UserUnavailable);
            }

            if (user.ActiveSessionId != identity.SessionId)
            {
                await ClearAsync();
                return new BrowserSessionRestoreResult(BrowserSessionRestoreStatus.ReplacedByNewLogin);
            }

            _userContext.SetCurrentUser(identity.UserId);
            return new BrowserSessionRestoreResult(BrowserSessionRestoreStatus.Restored);
        }
        catch
        {
            await ClearAsync();
            return new BrowserSessionRestoreResult(BrowserSessionRestoreStatus.UserUnavailable);
        }
    }

    public async Task<string?> GetCurrentSessionTokenAsync()
    {
        await ClearLegacyCurrentUserAsync();

        var token = await _js.InvokeAsync<string?>("sessionStorage.getItem", SessionTokenKey);
        if (_tokens.TryReadToken(token, out var identity) == BrowserSessionTokenService.BrowserSessionTokenStatus.Valid)
        {
            _db.ChangeTracker.Clear();
            var user = _db.Users.AsNoTracking().FirstOrDefault(item => item.UserId == identity.UserId && item.DeletedAt == null);
            if (user?.ActiveSessionId == identity.SessionId)
            {
                return token;
            }
        }

        await ClearAsync();
        return null;
    }

    public async Task SignOutAsync()
    {
        var token = await _js.InvokeAsync<string?>("sessionStorage.getItem", SessionTokenKey);
        if (_tokens.TryReadToken(token, out var identity) == BrowserSessionTokenService.BrowserSessionTokenStatus.Valid)
        {
            var user = _db.Users.FirstOrDefault(item => item.UserId == identity.UserId && item.DeletedAt == null);
            if (user is not null && user.ActiveSessionId == identity.SessionId)
            {
                _db.Users.Entry(user).CurrentValues.SetValues(user with
                {
                    ActiveSessionId = null,
                    UpdatedAt = DateTime.UtcNow
                });
                _db.SaveChanges();
            }
        }

        await SignOutLocalAsync();
    }

    public async Task SignOutLocalAsync()
    {
        _userContext.SignOut();
        await ClearAsync();
    }

    public async Task ClearAsync()
    {
        await _js.InvokeVoidAsync("sessionStorage.removeItem", SessionTokenKey);
        await ClearLegacyCurrentUserAsync();
    }

    private async Task ClearLegacyCurrentUserAsync()
        => await _js.InvokeVoidAsync("sessionStorage.removeItem", LegacyCurrentUserKey);
}

public sealed record BrowserSessionRestoreResult(BrowserSessionRestoreStatus Status)
{
    public bool IsAuthenticated => Status is BrowserSessionRestoreStatus.Restored or BrowserSessionRestoreStatus.AlreadyAuthenticated;
}

public enum BrowserSessionRestoreStatus
{
    MissingOrInvalid,
    Expired,
    ReplacedByNewLogin,
    UserUnavailable,
    Restored,
    AlreadyAuthenticated
}
