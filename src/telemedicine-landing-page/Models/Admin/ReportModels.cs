namespace TelemedicineLandingPage.Models.Admin;

/// <summary>One row of the consumption report (định mức vs thực tế).</summary>
public sealed record ConsumptionReportRow(
    string TechnicalServiceCode,
    string TechnicalServiceName,
    string ResourceCode,
    string ResourceName,
    string Unit,
    decimal StandardQuantity,
    decimal ActualQuantity,
    decimal Variance,
    decimal VariancePercent,
    string Period);

/// <summary>Trend direction for the dashboard KPIs.</summary>
public enum TrendDirection
{
    Up,
    Down,
    Flat,
}

/// <summary>One KPI tile rendered on the dashboard.</summary>
public sealed record DashboardKpi(
    string Label,
    string Value,
    double TrendPercent,
    TrendDirection TrendDirection,
    string Tone,
    string Icon,
    IReadOnlyList<int> Sparkline);

/// <summary>Severity tag used by the activity feed.</summary>
public enum ActivitySeverity
{
    Info,
    Warning,
    Critical,
}

/// <summary>One entry in the activity feed.</summary>
public sealed record ActivityEntry(
    DateTime Timestamp,
    string Actor,
    string Action,
    string Subject,
    ActivitySeverity Severity);

/// <summary>Notification record (used by INotificationService and the bell flyout).</summary>
public sealed record Notification(
    Guid Id,
    string Title,
    string Body,
    ActivitySeverity Severity,
    DateTime Timestamp,
    bool IsRead);
