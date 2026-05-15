using TelemedicineLandingPage.Models.Admin;

namespace TelemedicineLandingPage.Services.Admin;

/// <summary>Persists user-level preferences (theme, density, AI prompt, etc.).</summary>
public interface IUserPreferencesService
{
    UserPreferences Current { get; }
    void Update(UserPreferences next);
    event Action? StateChanged;
}
