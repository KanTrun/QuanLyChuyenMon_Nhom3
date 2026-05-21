using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace TelemedicineLandingPage.Components.Shared.Base;

public abstract class AuthorizedComponentBase : ComponentBase
{
    [Inject] protected IAuthorizationService AuthorizationService { get; set; } = default!;
    [Inject] protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] protected NavigationManager Navigation { get; set; } = default!;

    protected async Task<bool> EnsurePolicyAsync(string policyName)
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var result = await AuthorizationService.AuthorizeAsync(authState.User, policyName);
        if (result.Succeeded)
        {
            return true;
        }

        Navigation.NavigateTo($"/AccessDenied?policy={Uri.EscapeDataString(policyName)}", forceLoad: false);
        return false;
    }
}
