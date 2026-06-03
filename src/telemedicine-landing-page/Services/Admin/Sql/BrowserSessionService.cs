using Microsoft.JSInterop;
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

    public BrowserSessionService(
        IJSRuntime js,
        ICurrentUserContext userContext,
        BrowserSessionTokenService tokens)
    {
        _js = js;
        _userContext = userContext;
        _tokens = tokens;
    }

    public async Task PersistCurrentUserAsync()
    {
        var userId = _userContext.CurrentUser?.UserId;
        if (!userId.HasValue)
        {
            return;
        }

        await _js.InvokeVoidAsync("sessionStorage.setItem", SessionTokenKey, _tokens.IssueToken(userId.Value));
        await ClearLegacyCurrentUserAsync();
    }

    public async Task<bool> RestoreCurrentUserAsync()
    {
        await ClearLegacyCurrentUserAsync();

        if (_userContext.CurrentUser is not null)
        {
            return true;
        }

        var token = await _js.InvokeAsync<string?>("sessionStorage.getItem", SessionTokenKey);
        if (!_tokens.TryValidateToken(token, out var userId))
        {
            await ClearAsync();
            return false;
        }

        try
        {
            _userContext.SetCurrentUser(userId);
            return true;
        }
        catch
        {
            await ClearAsync();
            return false;
        }
    }

    public async Task<string?> GetCurrentSessionTokenAsync()
    {
        await ClearLegacyCurrentUserAsync();

        var token = await _js.InvokeAsync<string?>("sessionStorage.getItem", SessionTokenKey);
        if (_tokens.TryValidateToken(token, out _))
        {
            return token;
        }

        await ClearAsync();
        return null;
    }

    public async Task SignOutAsync()
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
