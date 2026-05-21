using Microsoft.AspNetCore.Components;

namespace TelemedicineLandingPage.Components;

public sealed class RedirectToAccessDenied : ComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    protected override void OnInitialized()
    {
        Navigation.NavigateTo("/AccessDenied", forceLoad: false);
    }
}
