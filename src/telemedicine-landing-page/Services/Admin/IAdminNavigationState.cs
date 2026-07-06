using TelemedicineLandingPage.Models.Admin;

namespace TelemedicineLandingPage.Services.Admin;

/// <summary>
/// Per-circuit state that coordinates the admin shell: sidebar collapse, command palette,
/// chatbot visibility, notification badge, navigation map and palette commands.
/// </summary>
public interface IAdminNavigationState
{
    bool IsSidebarCollapsed { get; }
    bool IsPaletteOpen { get; }
    bool IsChatbotOpen { get; }
    int UnreadNotifications { get; }

    IReadOnlyList<AdminNavItem> NavItems { get; }
    IReadOnlyList<PaletteCommand> Commands { get; }
    IReadOnlyDictionary<int, string> HotkeyMap { get; }
    IReadOnlyList<AdminNotificationStub> NotificationPreviews { get; }

    void ToggleSidebar();
    void OpenPalette();
    void ClosePalette();
    void TogglePalette();
    void OpenChatbot();
    void CloseChatbot();
    void ToggleChatbot();
    void MarkNotificationRead(Guid notificationId);
    void MarkAllNotificationsRead();

    event Action? StateChanged;
}
