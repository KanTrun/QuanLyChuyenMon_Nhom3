using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TelemedicineLandingPage.Models.Admin.Sql;

/// <summary>Tùy chọn thông báo của người dùng.</summary>
[Table("notification_preferences", Schema = "med")]
public sealed record NotificationPreference
{
    [Key]
    [Column("notification_preference_id")]
    public Guid NotificationPreferenceId { get; init; } = Guid.NewGuid();

    [Column("user_id")]
    public required Guid UserId { get; init; }

    [Column("notification_type")]
    public required string NotificationType { get; init; }

    [Column("channel_code")]
    public required string ChannelCode { get; init; }

    [Column("is_enabled")]
    public bool IsEnabled { get; init; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Thông báo gửi đến người dùng.</summary>
[Table("notifications", Schema = "med")]
public sealed record MedNotification
{
    [Key]
    [Column("notification_id")]
    public Guid NotificationId { get; init; } = Guid.NewGuid();

    [Column("recipient_user_id")]
    public required Guid RecipientUserId { get; init; }

    [Column("notification_type")]
    public required string NotificationType { get; init; }

    [Column("title")]
    public required string Title { get; init; }

    [Column("body")]
    public string? Body { get; init; }

    [Column("severity")]
    public string Severity { get; init; } = "info";

    [Column("source_type")]
    public string? SourceType { get; init; }

    [Column("source_id")]
    public string? SourceId { get; init; }

    [Column("payload_json")]
    public string? PayloadJson { get; init; }

    [Column("read_at")]
    public DateTime? ReadAt { get; init; }

    [Column("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Lần thử gửi thông báo qua kênh cụ thể.</summary>
[Table("notification_delivery_attempts", Schema = "med")]
public sealed record NotificationDeliveryAttempt
{
    [Key]
    [Column("notification_delivery_attempt_id")]
    public Guid NotificationDeliveryAttemptId { get; init; } = Guid.NewGuid();

    [Column("notification_id")]
    public required Guid NotificationId { get; init; }

    [Column("channel_code")]
    public required string ChannelCode { get; init; }

    [Column("delivery_status")]
    public required string DeliveryStatus { get; init; }

    [Column("attempted_at")]
    public DateTime AttemptedAt { get; init; } = DateTime.UtcNow;

    [Column("error_message")]
    public string? ErrorMessage { get; init; }
}
