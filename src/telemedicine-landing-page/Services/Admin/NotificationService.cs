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
        return new List<Notification>();
    }
}
