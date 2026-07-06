using Microsoft.AspNetCore.Authorization;

namespace TelemedicineLandingPage.Services.Auth;

public static class PermissionPolicyCatalog
{
    public static void Register(AuthorizationOptions options)
    {
        options.AddPolicy("AdminAccess", policy => policy.RequireAuthenticatedUser());
        options.AddPolicy("CanManageUsers", policy =>
            policy.Requirements.Add(new PermissionRequirement("SCR_ORG_USERS:UPDATE", "manage_users")));
        options.AddPolicy("CanManagePermissions", policy =>
            policy.Requirements.Add(new PermissionRequirement("SCR_PERMISSIONS:UPDATE", "manage_permissions")));
        options.AddPolicy("CanViewReports", policy =>
            policy.Requirements.Add(new PermissionRequirement("SCR_REPORTS:VIEW", "view_reports")));
        options.AddPolicy("CanApproveProcedures", policy =>
            policy.Requirements.Add(new PermissionRequirement("SCR_PROCEDURES:APPROVE", "approve_procedures")));
        options.AddPolicy("CanPublishProcedures", policy =>
            policy.Requirements.Add(new PermissionRequirement("SCR_PROCEDURES:PUBLISH", "publish_procedures")));
    }
}
