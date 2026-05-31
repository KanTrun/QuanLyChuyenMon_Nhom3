using Hangfire.Dashboard;
using TelemedicineLandingPage.Services.Auth;

namespace TelemedicineLandingPage.Infrastructure.Jobs;

public sealed class HangfireAdminAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var user = context.GetHttpContext().User;
        if (user.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        return user.HasClaim(PermissionClaimTypes.Permission, "SCR_SYSTEM:ADMIN")
            || user.HasClaim(PermissionClaimTypes.Permission, "SCR_ORG_USERS:UPDATE")
            || user.HasClaim(PermissionClaimTypes.Permission, "manage_users");
    }
}
