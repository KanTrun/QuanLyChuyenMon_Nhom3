namespace TelemedicineLandingPage.Models.Admin;

/// <summary>
/// Represents a single navigation entry rendered by the admin sidebar.
/// </summary>
public sealed record AdminNavItem(
    string Label,
    string Url,
    string Icon,
    string? Hotkey,
    IReadOnlyList<AdminNavItem>? Children = null);

/// <summary>
/// A single command exposed by the command palette.
/// Either NavigateTo (a URL) or Action (a callback) should be set; if both, Action wins.
/// </summary>
public sealed record PaletteCommand(
    string Group,
    string Label,
    string? Description,
    string? Hotkey,
    string? NavigateTo,
    Func<Task>? Action);

/// <summary>
/// Lightweight notification preview shown inside the top-bar bell flyout.
/// </summary>
public sealed record AdminNotificationStub(
    string Title,
    string Body,
    string Timestamp);
