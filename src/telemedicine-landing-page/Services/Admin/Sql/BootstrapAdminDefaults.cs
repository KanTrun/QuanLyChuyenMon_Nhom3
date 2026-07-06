namespace TelemedicineLandingPage.Services.Admin.Sql;

public static class BootstrapAdminDefaults
{
    public const string Username = "admin";
    public const string LocalDevelopmentPassword = "Admin@2026";

    public static string PasswordHash => CurrentUserContext.HashPassword(LocalDevelopmentPassword);
}
