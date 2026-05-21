namespace TelemedicineLandingPage.Services.Notifications;

public sealed record NotificationMessage(
    string NotificationType,
    string Title,
    string? Body = null,
    string Severity = "info",
    string? SourceType = null,
    string? SourceId = null,
    string? PayloadJson = null);

public sealed record NotificationEnvelope(
    Guid NotificationId,
    Guid? RecipientUserId,
    string NotificationType,
    string Title,
    string? Body,
    string Severity,
    DateTime CreatedAt);
