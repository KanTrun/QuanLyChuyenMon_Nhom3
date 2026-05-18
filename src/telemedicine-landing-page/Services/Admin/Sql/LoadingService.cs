namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>Dịch vụ quản lý trạng thái loading toàn cục.</summary>
public sealed class LoadingService
{
    public bool IsLoading { get; private set; }
    public string? Message { get; private set; }
    public event Action? StateChanged;

    /// <summary>Hiện loading overlay với thông báo tùy chọn.</summary>
    public void Show(string? message = null)
    {
        IsLoading = true;
        Message = message;
        StateChanged?.Invoke();
    }

    /// <summary>Ẩn loading overlay.</summary>
    public void Hide()
    {
        IsLoading = false;
        Message = null;
        StateChanged?.Invoke();
    }

    /// <summary>Thực thi hành động async với loading overlay.</summary>
    public async Task RunAsync(Func<Task> action, string? message = null)
    {
        Show(message);
        try { await action(); }
        finally { Hide(); }
    }
}
