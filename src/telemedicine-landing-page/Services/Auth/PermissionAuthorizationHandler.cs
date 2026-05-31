using Microsoft.AspNetCore.Authorization;

namespace TelemedicineLandingPage.Services.Auth;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionService _permissions;

    public PermissionAuthorizationHandler(IPermissionService permissions)
    {
        _permissions = permissions;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (_permissions.HasPermission(context.User, requirement.PermissionCodes.ToArray()))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
