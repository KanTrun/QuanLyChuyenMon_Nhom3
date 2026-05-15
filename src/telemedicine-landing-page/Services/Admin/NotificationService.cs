using TelemedicineLandingPage.Models.Admin;

namespace TelemedicineLandingPage.Services.Admin;

public sealed class NotificationService : INotificationService
{
    private readonly object _gate = new();
    private readonly List<Notification> _items;

    public NotificationService()
    {
        _items = SeedData();
    }

    public event Action? StateChanged;

    public int UnreadCount
    {
        get
        {
            lock (_gate) return _items.Count(n => !n.IsRead);
        }
    }

    public IReadOnlyList<Notification> ListAll()
    {
        lock (_gate) return _items.OrderByDescending(n => n.Timestamp).ToList();
    }

    public IReadOnlyList<Notification> ListUnread()
    {
        lock (_gate) return _items.Where(n => !n.IsRead).OrderByDescending(n => n.Timestamp).ToList();
    }

    public void MarkRead(Guid id)
    {
        lock (_gate)
        {
            var index = _items.FindIndex(n => n.Id == id);
            if (index < 0) return;
            if (_items[index].IsRead) return;
            _items[index] = _items[index] with { IsRead = true };
        }
        Raise();
    }

    public void MarkAllRead()
    {
        var changed = false;
        lock (_gate)
        {
            for (var i = 0; i < _items.Count; i++)
            {
                if (!_items[i].IsRead)
                {
                    _items[i] = _items[i] with { IsRead = true };
                    changed = true;
                }
            }
        }
        if (changed) Raise();
    }

    public Notification AddNotification(string title, string body, ActivitySeverity severity)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Tiêu đề không được để trống.", nameof(title));
        var entry = new Notification(
            Guid.NewGuid(),
            title.Trim(),
            string.IsNullOrWhiteSpace(body) ? string.Empty : body.Trim(),
            severity,
            DateTime.Now,
            IsRead: false);
        lock (_gate) _items.Insert(0, entry);
        Raise();
        return entry;
    }

    private void Raise() => StateChanged?.Invoke();

    private static List<Notification> SeedData()
    {
        var now = DateTime.Now;
        return new List<Notification>
        {
            new(Guid.NewGuid(), "Quy trình mới chờ phê duyệt", "Quy trình tiêm vaccine COVID-19 đang chờ duyệt.", ActivitySeverity.Warning, now.AddMinutes(-2), false),
            new(Guid.NewGuid(), "Báo cáo tiêu thụ tuần", "Báo cáo tiêu thụ vật tư tuần 19 đã sẵn sàng.", ActivitySeverity.Info, now.AddMinutes(-12), false),
            new(Guid.NewGuid(), "Cập nhật phân quyền", "Tài khoản BS. Lê Quang Huy được cấp quyền Lãnh đạo khoa.", ActivitySeverity.Info, now.AddHours(-1), false),
            new(Guid.NewGuid(), "Cảnh báo nhiễm khuẩn", "Phòng mổ số 2 ghi nhận chỉ số ATP cao hơn ngưỡng.", ActivitySeverity.Critical, now.AddHours(-3), true),
            new(Guid.NewGuid(), "Tăng đột biến yêu cầu xét nghiệm", "Khoa Xét nghiệm cần bổ sung 2 kỹ thuật viên ca chiều.", ActivitySeverity.Warning, now.AddHours(-5), true),
        };
    }
}
