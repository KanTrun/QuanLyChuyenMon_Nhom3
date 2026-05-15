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
    event Action? ExportConsumptionRequested;

    void RequestToggle();
    void RequestExportConsumption();
}

public sealed class ThemeBus : IThemeBus
{
    public event Action? ToggleRequested;
    public event Action? ExportConsumptionRequested;

    public void RequestToggle() => ToggleRequested?.Invoke();
    public void RequestExportConsumption() => ExportConsumptionRequested?.Invoke();
}
