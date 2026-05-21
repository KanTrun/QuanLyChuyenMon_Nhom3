using Microsoft.JSInterop;

namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>Persists the current user id in browser session storage for Blazor circuit reloads.</summary>
public sealed class BrowserSessionService
{
    private const string CurrentUserKey = "qlcm_uid";

    private readonly IJSRuntime _js;
    private readonly ICurrentUserContext _userContext;

    public BrowserSessionService(IJSRuntime js, ICurrentUserContext userContext)
    {
        _js = js;
        _userContext = userContext;
    }

    public async Task PersistCurrentUserAsync()
    {
        var userId = _userContext.CurrentUser?.UserId;
        if (!userId.HasValue)
        {
            return;
        }

        await _js.InvokeVoidAsync("sessionStorage.setItem", CurrentUserKey, userId.Value.ToString());
    }

    public async Task<bool> RestoreCurrentUserAsync()
    {
        if (_userContext.CurrentUser is not null)
        {
            return true;
        }

        var rawUserId = await _js.InvokeAsync<string?>("sessionStorage.getItem", CurrentUserKey);
        if (!Guid.TryParse(rawUserId, out var userId))
        {
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

    public async Task SignOutAsync()
    {
        _userContext.SignOut();
        await ClearAsync();
    }

    public async Task ClearAsync()
        => await _js.InvokeVoidAsync("sessionStorage.removeItem", CurrentUserKey);
}
