namespace TelemedicineLandingPage.Models.Admin;

/// <summary>Density preference for the admin shell (tighter vs roomier rows).</summary>
public enum AdminDensity
{
    Compact,
    Comfortable,
}

/// <summary>Per-channel notification toggles surfaced on the Cài đặt page.</summary>
public sealed record NotificationChannelPrefs(
    bool InApp,
    bool Email,
    bool Sms);

/// <summary>User-level preferences for the QLCM Pro admin shell.</summary>
public sealed record UserPreferences
{
    public string FullName { get; init; } = "BS. Nguyễn Văn A";
    public string Email { get; init; } = "nguyen.van.a@qlcm.local";
    public Department Department { get; init; } = Department.NoiTiet;
    public string Theme { get; init; } = "light";
    public AdminDensity Density { get; init; } = AdminDensity.Comfortable;
    public bool AnimationsEnabled { get; init; } = true;
    public NotificationChannelPrefs Notifications { get; init; } = new(true, true, false);
    public string AiModel { get; init; } = "claude-sonnet-4-5-20250929";
    public double AiTemperature { get; init; } = 0.4;
    public string AiSystemPrompt { get; init; } =
        "Bạn là trợ lý nội bộ của bệnh viện QLCM Pro, trả lời ngắn gọn bằng tiếng Việt và luôn bám theo quy trình chuẩn của bệnh viện.";
}
