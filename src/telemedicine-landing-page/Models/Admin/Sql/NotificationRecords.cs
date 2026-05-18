namespace TelemedicineLandingPage.Models.Admin.Sql;

/// <summary>Tùy chọn thông báo của người dùng.</summary>
public sealed record NotificationPreference
{
    public Guid NotificationPreferenceId { get; init; } = Guid.NewGuid();
    public required Guid UserId { get; init; }
    public required string NotificationType { get; init; }
    public required string ChannelCode { get; init; }
    public bool IsEnabled { get; init; } = true;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Thông báo gửi đến người dùng.</summary>
public sealed record MedNotification
{
    public Guid NotificationId { get; init; } = Guid.NewGuid();
    public required Guid RecipientUserId { get; init; }
    public required string NotificationType { get; init; }
    public required string Title { get; init; }
    public string? Body { get; init; }
    public string Severity { get; init; } = "info";
    public string? SourceType { get; init; }
    public string? SourceId { get; init; }
    public string? PayloadJson { get; init; }
    public DateTime? ReadAt { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Lần thử gửi thông báo qua kênh cụ thể.</summary>
public sealed record NotificationDeliveryAttempt
{
    public Guid NotificationDeliveryAttemptId { get; init; } = Guid.NewGuid();
    public required Guid NotificationId { get; init; }
    public required string ChannelCode { get; init; }
    public required string DeliveryStatus { get; init; }
    public DateTime AttemptedAt { get; init; } = DateTime.UtcNow;
    public string? ErrorMessage { get; init; }
}
