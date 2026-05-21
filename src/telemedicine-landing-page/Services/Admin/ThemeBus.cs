namespace TelemedicineLandingPage.Services.Admin;

/// <summary>
/// Per-circuit bus that lets the command palette ask the AdminTopBar to flip the
/// theme: the top bar holds the IJSRuntime reference needed for localStorage,
/// while the palette command lives inside the navigation state. The bus avoids
/// passing IJSRuntime through every call site.
/// </summary>
public interface IThemeBus
{
    event Action? ToggleRequested;
    event Action<string>? ThemeChanged;
    event Action<bool>? MotionChanged;
    event Action? ExportConsumptionRequested;

    void RequestToggle();
    void SetTheme(string theme);
    void SetMotion(bool enabled);
    void RequestExportConsumption();
}

public sealed class ThemeBus : IThemeBus
{
    public event Action? ToggleRequested;
    public event Action<string>? ThemeChanged;
    public event Action<bool>? MotionChanged;
    public event Action? ExportConsumptionRequested;

    public void RequestToggle() => ToggleRequested?.Invoke();
    public void SetTheme(string theme) => ThemeChanged?.Invoke(theme == "dark" ? "dark" : "light");
    public void SetMotion(bool enabled) => MotionChanged?.Invoke(enabled);
    public void RequestExportConsumption() => ExportConsumptionRequested?.Invoke();
}
