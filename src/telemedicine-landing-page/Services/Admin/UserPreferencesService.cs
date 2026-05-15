using TelemedicineLandingPage.Models.Admin;

namespace TelemedicineLandingPage.Services.Admin;

public sealed class UserPreferencesService : IUserPreferencesService
{
    private readonly object _gate = new();
    private UserPreferences _current = new();

    public UserPreferences Current
    {
        get { lock (_gate) return _current; }
    }

    public void Update(UserPreferences next)
    {
        ArgumentNullException.ThrowIfNull(next);
        lock (_gate)
        {
            _current = next;
        }
        StateChanged?.Invoke();
    }

    public event Action? StateChanged;
}
