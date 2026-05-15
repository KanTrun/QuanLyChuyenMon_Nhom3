using System.Globalization;
using TelemedicineLandingPage.Models.Admin;

namespace TelemedicineLandingPage.Services.Admin;

/// <summary>
/// Default scoped implementation of <see cref="IAdminNavigationState"/>. The
/// navigation tree and palette command catalogue are seeded once in the
/// constructor; the notification badge and previews are delegated to
/// <see cref="INotificationService"/>.
/// </summary>
public sealed class AdminNavigationState : IAdminNavigationState, IDisposable
{
    private static readonly IReadOnlyList<AdminNavItem> SeededNavItems = BuildNavItems();
    private static readonly IReadOnlyDictionary<int, string> SeededHotkeyMap = new Dictionary<int, string>
    {
        [0] = "/admin",
        [1] = "/admin/quy-trinh",
        [2] = "/admin/phan-quyen",
        [3] = "/admin/danh-muc",
        [4] = "/admin/phac-do",
        [5] = "/admin/bao-cao",
        [6] = "/admin/cai-dat",
    };

    private readonly INotificationService _notifications;
    private readonly IThemeBus _themeBus;

    public AdminNavigationState(INotificationService notifications, IThemeBus themeBus)
    {
        _notifications = notifications;
        _themeBus = themeBus;
        Commands = BuildCommands(this);
        _notifications.StateChanged += OnNotificationsChanged;
    }

    public bool IsSidebarCollapsed { get; private set; }
    public bool IsPaletteOpen { get; private set; }
    public bool IsChatbotOpen { get; private set; }

    public int UnreadNotifications => _notifications.UnreadCount;

    public IReadOnlyList<AdminNavItem> NavItems => SeededNavItems;
    public IReadOnlyList<PaletteCommand> Commands { get; }
    public IReadOnlyDictionary<int, string> HotkeyMap => SeededHotkeyMap;

    public IReadOnlyList<AdminNotificationStub> NotificationPreviews =>
        _notifications.ListAll()
            .Take(4)
            .Select(n => new AdminNotificationStub(n.Title, n.Body, FormatRelative(n.Timestamp)))
            .ToList();

    public event Action? StateChanged;

    public void ToggleSidebar()
    {
        IsSidebarCollapsed = !IsSidebarCollapsed;
        Raise();
    }

    public void OpenPalette()
    {
        if (IsPaletteOpen) return;
        IsPaletteOpen = true;
        Raise();
    }

    public void ClosePalette()
    {
        if (!IsPaletteOpen) return;
        IsPaletteOpen = false;
        Raise();
    }

    public void TogglePalette()
    {
        IsPaletteOpen = !IsPaletteOpen;
        Raise();
    }

    public void OpenChatbot()
    {
        if (IsChatbotOpen) return;
        IsChatbotOpen = true;
        Raise();
    }

    public void CloseChatbot()
    {
        if (!IsChatbotOpen) return;
        IsChatbotOpen = false;
        Raise();
    }

    public void ToggleChatbot()
    {
        IsChatbotOpen = !IsChatbotOpen;
        Raise();
    }

    public void Dispose()
    {
        _notifications.StateChanged -= OnNotificationsChanged;
    }

    private void OnNotificationsChanged() => Raise();

    private void Raise() => StateChanged?.Invoke();

    private static string FormatRelative(DateTime when)
    {
        var delta = DateTime.Now - when;
        if (delta.TotalSeconds < 90) return "Vừa xong";
        if (delta.TotalMinutes < 60) return $"{(int)delta.TotalMinutes} phút trước";
        if (delta.TotalHours < 24) return $"{(int)delta.TotalHours} giờ trước";
        if (delta.TotalDays < 7) return $"{(int)delta.TotalDays} ngày trước";
        return when.ToString("dd/MM HH:mm", CultureInfo.GetCultureInfo("vi-VN"));
    }

    private static IReadOnlyList<AdminNavItem> BuildNavItems()
    {
        return new List<AdminNavItem>
        {
            new("Tổng quan", "/admin", "dashboard", "Alt+0"),
            new("Quy trình", "/admin/quy-trinh", "workflow", "Alt+1", new List<AdminNavItem>
            {
                new("Quy trình kỹ thuật", "/admin/quy-trinh", "list", null),
                new("Tạo mới", "/admin/quy-trinh/tao", "plus", null),
                new("Phê duyệt", "/admin/quy-trinh/phe-duyet", "check", null),
            }),
            new("Phân quyền", "/admin/phan-quyen", "shield", "Alt+2"),
            new("Danh mục", "/admin/danh-muc", "catalog", "Alt+3"),
            new("Phác đồ", "/admin/phac-do", "stethoscope", "Alt+4"),
            new("Báo cáo", "/admin/bao-cao", "chart", "Alt+5", new List<AdminNavItem>
            {
                new("Tổng hợp", "/admin/bao-cao", "chart", null),
                new("Báo cáo tiêu thụ", "/admin/bao-cao/tieu-thu", "package", null),
            }),
            new("Lâm sàng", "/admin/lam-sang", "heart", null),
            new("Cài đặt", "/admin/cai-dat", "settings", "Alt+6"),
        };
    }

    private static IReadOnlyList<PaletteCommand> BuildCommands(AdminNavigationState state)
    {
        var commands = new List<PaletteCommand>();

        foreach (var item in state.NavItems)
        {
            if (item.Children is { Count: > 0 } children)
            {
                foreach (var child in children)
                {
                    commands.Add(new PaletteCommand(
                        Group: "Điều hướng",
                        Label: child.Label,
                        Description: item.Label,
                        Hotkey: child.Url == item.Url ? item.Hotkey : null,
                        NavigateTo: child.Url,
                        Action: null));
                }
            }
            else
            {
                commands.Add(new PaletteCommand(
                    Group: "Điều hướng",
                    Label: item.Label,
                    Description: "Khu vực quản trị",
                    Hotkey: item.Hotkey,
                    NavigateTo: item.Url,
                    Action: null));
            }
        }

        commands.Add(new PaletteCommand(
            Group: "Hành động nhanh",
            Label: "Tạo quy trình mới",
            Description: "Mở biểu mẫu tạo quy trình kỹ thuật",
            Hotkey: null,
            NavigateTo: "/admin/quy-trinh/tao",
            Action: null));

        commands.Add(new PaletteCommand(
            Group: "Hành động nhanh",
            Label: "Phê duyệt quy trình chờ",
            Description: "Mở danh sách quy trình đang chờ duyệt",
            Hotkey: null,
            NavigateTo: "/admin/quy-trinh/phe-duyet",
            Action: null));

        commands.Add(new PaletteCommand(
            Group: "Hành động nhanh",
            Label: "Đánh dấu tất cả thông báo đã đọc",
            Description: "Xóa số đếm thông báo trên thanh công cụ",
            Hotkey: null,
            NavigateTo: null,
            Action: () => { state._notifications.MarkAllRead(); return Task.CompletedTask; }));

        commands.Add(new PaletteCommand(
            Group: "Hành động nhanh",
            Label: "Xuất báo cáo tiêu thụ",
            Description: "Tải báo cáo tiêu thụ vật tư dạng CSV",
            Hotkey: null,
            NavigateTo: null,
            Action: () => { state._themeBus.RequestExportConsumption(); return Task.CompletedTask; }));

        commands.Add(new PaletteCommand(
            Group: "Hành động nhanh",
            Label: "Mở trợ lý AI",
            Description: "Bật bảng trò chuyện trợ lý nội bộ",
            Hotkey: "Ctrl+/",
            NavigateTo: null,
            Action: () => { state.OpenChatbot(); return Task.CompletedTask; }));

        commands.Add(new PaletteCommand(
            Group: "Hành động nhanh",
            Label: "Chuyển chế độ tối/sáng",
            Description: "Đảo giao diện sáng và tối",
            Hotkey: null,
            NavigateTo: null,
            Action: () => { state._themeBus.RequestToggle(); return Task.CompletedTask; }));

        commands.Add(new PaletteCommand(
            Group: "Cài đặt",
            Label: "Mở Cài đặt",
            Description: "Tài khoản, giao diện và trợ lý AI",
            Hotkey: null,
            NavigateTo: "/admin/cai-dat",
            Action: null));

        return commands;
    }
}
