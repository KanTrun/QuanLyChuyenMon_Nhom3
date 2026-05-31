using Microsoft.AspNetCore.Identity;

namespace TelemedicineLandingPage.Models.Auth;

/// <summary>
/// Identity account used by ASP.NET Core authentication.
/// The existing `med.users` row remains the RBAC/domain user source of truth.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public Guid? MedUserId { get; set; }
    public string? FullName { get; set; }
    public string Status { get; set; } = "active";
}

public sealed class ApplicationRole : IdentityRole<Guid>
{
}
