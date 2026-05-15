namespace TelemedicineLandingPage.Services.Admin;

/// <summary>Visual variant of a toast notification.</summary>
public enum ToastVariant
{
    Info,
    Success,
    Warning,
    Error,
}

/// <summary>One transient toast rendered by the AdminToast container.</summary>
public sealed record ToastMessage(
    Guid Id,
    string Title,
    string? Body,
    ToastVariant Variant,
    DateTime IssuedAt);

/// <summary>Per-circuit toast bus used by the admin pages to surface success/failure messages.</summary>
public interface IToastService
{
    IReadOnlyList<ToastMessage> Active { get; }
    ToastMessage Show(string title, string? body = null, ToastVariant variant = ToastVariant.Info);
    void Dismiss(Guid id);

    event Action? StateChanged;
}

public sealed class ToastService : IToastService
{
    private readonly object _gate = new();
    private readonly List<ToastMessage> _active = new();

    public IReadOnlyList<ToastMessage> Active
    {
        get { lock (_gate) return _active.ToList(); }
    }

    public event Action? StateChanged;

    public ToastMessage Show(string title, string? body = null, ToastVariant variant = ToastVariant.Info)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            title = "Thông báo";
        }
        var message = new ToastMessage(Guid.NewGuid(), title.Trim(), string.IsNullOrWhiteSpace(body) ? null : body.Trim(), variant, DateTime.Now);
        lock (_gate)
        {
            _active.Add(message);
        }
        StateChanged?.Invoke();
        return message;
    }

    public void Dismiss(Guid id)
    {
        var changed = false;
        lock (_gate)
        {
            changed = _active.RemoveAll(t => t.Id == id) > 0;
        }
        if (changed) StateChanged?.Invoke();
    }
}
