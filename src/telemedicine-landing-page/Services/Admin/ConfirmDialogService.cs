namespace TelemedicineLandingPage.Services.Admin;

/// <summary>One pending confirmation prompt rendered by AdminConfirmDialog.</summary>
public sealed class ConfirmRequest
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Body { get; init; }
    public string ConfirmText { get; init; } = "Xác nhận";
    public string CancelText { get; init; } = "Hủy";
    public bool IsDanger { get; init; }
    public TaskCompletionSource<bool> Completion { get; } = new();
}

/// <summary>Per-circuit confirmation prompt bus.</summary>
public interface IConfirmDialogService
{
    ConfirmRequest? Pending { get; }
    Task<bool> ShowAsync(string title, string body, string confirmText = "Xác nhận", string cancelText = "Hủy", bool isDanger = false);
    void Resolve(Guid id, bool confirmed);

    event Action? StateChanged;
}

public sealed class ConfirmDialogService : IConfirmDialogService
{
    private readonly object _gate = new();
    private ConfirmRequest? _pending;

    public ConfirmRequest? Pending
    {
        get { lock (_gate) return _pending; }
    }

    public event Action? StateChanged;

    public Task<bool> ShowAsync(string title, string body, string confirmText = "Xác nhận", string cancelText = "Hủy", bool isDanger = false)
    {
        var request = new ConfirmRequest
        {
            Id = Guid.NewGuid(),
            Title = string.IsNullOrWhiteSpace(title) ? "Xác nhận" : title.Trim(),
            Body = body ?? string.Empty,
            ConfirmText = string.IsNullOrWhiteSpace(confirmText) ? "Xác nhận" : confirmText,
            CancelText = string.IsNullOrWhiteSpace(cancelText) ? "Hủy" : cancelText,
            IsDanger = isDanger,
        };
        lock (_gate)
        {
            // If something is already pending, settle it as cancelled before queuing the new one.
            _pending?.Completion.TrySetResult(false);
            _pending = request;
        }
        StateChanged?.Invoke();
        return request.Completion.Task;
    }

    public void Resolve(Guid id, bool confirmed)
    {
        ConfirmRequest? settled = null;
        lock (_gate)
        {
            if (_pending is null || _pending.Id != id) return;
            settled = _pending;
            _pending = null;
        }
        settled.Completion.TrySetResult(confirmed);
        StateChanged?.Invoke();
    }
}
