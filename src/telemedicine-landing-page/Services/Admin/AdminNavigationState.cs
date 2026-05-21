using System.Globalization;
using TelemedicineLandingPage.Models.Admin;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin;

/// <summary>
/// Per-circuit state for the admin shell. Navigation, hotkeys and notifications
/// all read from the SQL-backed QLCM data store.
/// </summary>
public sealed class AdminNavigationState : IAdminNavigationState, IDisposable
{
    private static readonly IReadOnlyList<AdminNavItem> SeededNavItems = BuildNavItems();
    private static readonly IReadOnlyDictionary<int, string> SeededHotkeyMap = new Dictionary<int, string>
    {
        [0] = "/admin",
        [1] = "/admin/quy-trinh",
        [2] = "/phe-duyet",
        [3] = "/tai-nguyen",
        [4] = "/dieu-phoi",
        [5] = "/admin/bao-cao",
        [6] = "/thong-bao",
    };

    private readonly IMedDataStore _store;
    private readonly ICurrentUserContext _userContext;
    private readonly IThemeBus _themeBus;

    public AdminNavigationState(IMedDataStore store, ICurrentUserContext userContext, IThemeBus themeBus)
    {
        _store = store;
        _userContext = userContext;
        _themeBus = themeBus;
        Commands = BuildCommands(this);
        _store.StateChanged += OnStoreChanged;
        _userContext.StateChanged += OnStoreChanged;
    }

    public bool IsSidebarCollapsed { get; private set; }
    public bool IsPaletteOpen { get; private set; }
    public bool IsChatbotOpen { get; private set; }
    public IReadOnlyList<AdminNavItem> NavItems => SeededNavItems;
    public IReadOnlyList<PaletteCommand> Commands { get; }
    public IReadOnlyDictionary<int, string> HotkeyMap => SeededHotkeyMap;

    public int UnreadNotifications
    {
        get
        {
            var userId = _userContext.CurrentUser?.UserId;
            return userId.HasValue
                ? _store.Notifications.Count(n => n.RecipientUserId == userId.Value && n.ReadAt is null)
                : 0;
        }
    }

    public IReadOnlyList<AdminNotificationStub> NotificationPreviews
    {
        get
        {
            var userId = _userContext.CurrentUser?.UserId;
            if (!userId.HasValue)
            {
                return Array.Empty<AdminNotificationStub>();
            }

            return _store.Notifications
                .Where(n => n.RecipientUserId == userId.Value)
                .OrderByDescending(n => n.CreatedAt)
                .Take(4)
                .Select(n => new AdminNotificationStub(
                    n.NotificationId,
                    n.Title,
                    n.Body ?? string.Empty,
                    FormatRelative(n.CreatedAt),
                    n.ReadAt is null,
                    n.Severity))
                .ToList();
        }
    }

    public event Action? StateChanged;

    public void ToggleSidebar() { IsSidebarCollapsed = !IsSidebarCollapsed; Raise(); }
    public void OpenPalette() { if (!IsPaletteOpen) { IsPaletteOpen = true; Raise(); } }
    public void ClosePalette() { if (IsPaletteOpen) { IsPaletteOpen = false; Raise(); } }
    public void TogglePalette() { IsPaletteOpen = !IsPaletteOpen; Raise(); }
    public void OpenChatbot() { if (!IsChatbotOpen) { IsChatbotOpen = true; Raise(); } }
    public void CloseChatbot() { if (IsChatbotOpen) { IsChatbotOpen = false; Raise(); } }
    public void ToggleChatbot() { IsChatbotOpen = !IsChatbotOpen; Raise(); }

    public void MarkNotificationRead(Guid notificationId)
    {
        var userId = _userContext.CurrentUser?.UserId;
        if (!userId.HasValue)
        {
            return;
        }

        var notification = _store.Notifications
            .FirstOrDefault(n => n.NotificationId == notificationId && n.RecipientUserId == userId.Value);
        if (notification is null || notification.ReadAt.HasValue)
        {
            return;
        }

        _store.UpdateNotificationReadAt(notification.NotificationId, DateTime.UtcNow);
    }

    public void MarkAllNotificationsRead()
    {
        var userId = _userContext.CurrentUser?.UserId;
        if (!userId.HasValue)
        {
            return;
        }

        var unreadIds = _store.Notifications
            .Where(n => n.RecipientUserId == userId.Value && n.ReadAt is null)
            .Select(n => n.NotificationId)
            .ToList();
        foreach (var notificationId in unreadIds)
        {
            MarkNotificationRead(notificationId);
        }
    }

    public void Dispose()
    {
        _store.StateChanged -= OnStoreChanged;
        _userContext.StateChanged -= OnStoreChanged;
    }

    private void OnStoreChanged() => Raise();
    private void Raise() => StateChanged?.Invoke();

    private static string FormatRelative(DateTime when)
    {
        var delta = DateTime.Now - when.ToLocalTime();
        if (delta.TotalSeconds < 90) return "Vừa xong";
        if (delta.TotalMinutes < 60) return $"{(int)delta.TotalMinutes} phút trước";
        if (delta.TotalHours < 24) return $"{(int)delta.TotalHours} giờ trước";
        if (delta.TotalDays < 7) return $"{(int)delta.TotalDays} ngày trước";
        return when.ToLocalTime().ToString("dd/MM HH:mm", CultureInfo.GetCultureInfo("vi-VN"));
    }

    private static IReadOnlyList<AdminNavItem> BuildNavItems()
    {
        return new List<AdminNavItem>
        {
            new("Tổng quan", "/admin", "dashboard", "Alt+0"),
            new("Tổ chức", "/admin/to-chuc", "team", null, new List<AdminNavItem>
            {
                new("Khoa/Phòng", "/admin/to-chuc/khoa-phong", "workflow", null),
                new("Người dùng", "/admin/to-chuc/nguoi-dung", "user", null),
                new("Vai trò", "/admin/to-chuc/vai-tro", "shield", null),
                new("Nhóm", "/admin/to-chuc/nhom", "catalog", null),
            }),
            new("Quy trình", "/admin/quy-trinh", "workflow", "Alt+1", new List<AdminNavItem>
            {
                new("Quy trình kỹ thuật", "/admin/quy-trinh", "list", null),
                new("Tạo mới", "/admin/quy-trinh/tao", "plus", null),
                new("Phê duyệt quy trình", "/admin/quy-trinh/phe-duyet", "check", null),
            }),
            new("Phân quyền", "/admin/phan-quyen", "shield", null),
            new("Phê duyệt quyền", "/phe-duyet", "check", "Alt+2"),
            new("Danh mục", "/admin/danh-muc", "catalog", null),
            new("Tài nguyên", "/tai-nguyen", "package", "Alt+3"),
            new("Điều phối", "/dieu-phoi", "workflow", "Alt+4"),
            new("Phác đồ", "/admin/phac-do", "stethoscope", null, new List<AdminNavItem>
            {
                new("Quản trị phác đồ", "/admin/phac-do", "stethoscope", null),
                new("Không gian phác đồ", "/phac-do-pro", "list", null),
            }),
            new("Lâm sàng", "/admin/lam-sang", "heart", null),
            new("Báo cáo", "/admin/bao-cao", "chart", "Alt+5", new List<AdminNavItem>
            {
                new("Tổng hợp", "/admin/bao-cao", "chart", null),
                new("Báo cáo tiêu thụ", "/admin/bao-cao/tieu-thu", "package", null),
            }),
            new("Thông báo", "/thong-bao", "bell", "Alt+6"),
            new("Nhật ký", "/admin/nhat-ky", "history", null),
            new("Màn hình hệ thống", "/admin/he-thong/man-hinh", "settings", null),
            new("Hồ sơ", "/admin/ho-so", "user", null),
            new("Cài đặt", "/admin/cai-dat", "settings", null),
        };
    }

    private static IReadOnlyList<PaletteCommand> BuildCommands(AdminNavigationState state)
    {
        var commands = new List<PaletteCommand>();
        foreach (var item in state.NavItems)
        {
            IEnumerable<AdminNavItem> entries = item.Children is { Count: > 0 } children ? children : new[] { item };
            foreach (var entry in entries)
            {
                commands.Add(new PaletteCommand("Điều hướng", entry.Label, item.Label, entry.Hotkey ?? item.Hotkey, entry.Url, null));
            }
        }

        commands.Add(new PaletteCommand("Hành động nhanh", "Tạo quy trình mới", "Mở biểu mẫu tạo quy trình kỹ thuật", null, "/admin/quy-trinh/tao", null));
        commands.Add(new PaletteCommand("Hành động nhanh", "Phê duyệt quy trình", "Mở danh sách quy trình chờ duyệt", null, "/admin/quy-trinh/phe-duyet", null));
        commands.Add(new PaletteCommand("Hành động nhanh", "Đánh dấu tất cả thông báo đã đọc", "Cập nhật thông báo của người dùng hiện tại", null, null, () => { state.MarkAllNotificationsRead(); return Task.CompletedTask; }));
        commands.Add(new PaletteCommand("Hành động nhanh", "Xuất báo cáo tiêu thụ", "Mở trang xuất báo cáo tiêu thụ vật tư", null, "/admin/bao-cao/tieu-thu?xuat=1", null));
        commands.Add(new PaletteCommand("Hành động nhanh", "Mở trợ lý AI", "Bật bảng trò chuyện trợ lý nội bộ", "Ctrl+/", null, () => { state.OpenChatbot(); return Task.CompletedTask; }));
        commands.Add(new PaletteCommand("Hành động nhanh", "Chuyển chế độ tối/sáng", "Đảo giao diện sáng và tối", null, null, () => { state._themeBus.RequestToggle(); return Task.CompletedTask; }));
        return commands;
    }
}
