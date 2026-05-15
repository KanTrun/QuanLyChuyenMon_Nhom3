using TelemedicineLandingPage.Models.Admin;

namespace TelemedicineLandingPage.Services.Admin;

/// <summary>
/// Default scoped implementation of <see cref="IAdminNavigationState"/>. The nav and
/// palette command collections are seeded once in the constructor; later features
/// may extend the palette by composing additional providers.
/// </summary>
public sealed class AdminNavigationState : IAdminNavigationState
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

    private static readonly IReadOnlyList<AdminNotificationStub> SeededNotifications =
    [
        new("Quy trình mới chờ phê duyệt", "Quy trình tiêm vaccine COVID-19 đang chờ duyệt.", "2 phút trước"),
        new("Báo cáo tiêu thụ tuần", "Báo cáo tiêu thụ vật tư tuần 19 đã sẵn sàng.", "12 phút trước"),
        new("Cập nhật phân quyền", "Tài khoản BS. Lê Quang Huy được cấp quyền Lãnh đạo khoa.", "1 giờ trước"),
    ];

    public AdminNavigationState()
    {
        Commands = BuildCommands(this);
    }

    public bool IsSidebarCollapsed { get; private set; }
    public bool IsPaletteOpen { get; private set; }
    public bool IsChatbotOpen { get; private set; }

    public int UnreadNotifications { get; private set; } = 3;

    public IReadOnlyList<AdminNavItem> NavItems => SeededNavItems;
    public IReadOnlyList<PaletteCommand> Commands { get; }
    public IReadOnlyDictionary<int, string> HotkeyMap => SeededHotkeyMap;
    public IReadOnlyList<AdminNotificationStub> NotificationPreviews => SeededNotifications;

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

    private void Raise() => StateChanged?.Invoke();

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

        // Group: Điều hướng - one entry per leaf nav item.
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

        // Group: Hành động nhanh
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
            // Theme toggle is wired from the top-bar (where IJSRuntime is available);
            // FEAT-003 will replace this no-op with a real handler.
            Action: () => Task.CompletedTask));

        // Group: Cài đặt
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
