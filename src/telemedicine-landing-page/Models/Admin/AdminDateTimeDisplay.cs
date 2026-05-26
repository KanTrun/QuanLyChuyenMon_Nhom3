using System.Globalization;

namespace TelemedicineLandingPage.Models.Admin;

/// <summary>
/// Formats UTC timestamps consistently for the Vietnamese admin UI.
/// </summary>
public static class AdminDateTimeDisplay
{
    private static readonly CultureInfo ViCulture = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly TimeZoneInfo VietnamTimeZone = ResolveVietnamTimeZone();
    private static readonly string[] ViDays =
    [
        "Ch\u1ee7 Nh\u1eadt",
        "Th\u1ee9 Hai",
        "Th\u1ee9 Ba",
        "Th\u1ee9 T\u01b0",
        "Th\u1ee9 N\u0103m",
        "Th\u1ee9 S\u00e1u",
        "Th\u1ee9 B\u1ea3y"
    ];

    public static DateTime Now()
        => TimeZoneInfo.ConvertTimeFromUtc(global::System.DateTime.UtcNow, VietnamTimeZone);

    public static DateOnly Today()
        => DateOnly.FromDateTime(Now());

    public static DateTime ToDisplayTime(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Utc ? value : global::System.DateTime.SpecifyKind(value, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, VietnamTimeZone);
    }

    public static DateTime DisplayTimeToUtc(DateTime value)
    {
        if (value.Kind == DateTimeKind.Utc)
        {
            return value;
        }

        return TimeZoneInfo.ConvertTimeToUtc(global::System.DateTime.SpecifyKind(value, DateTimeKind.Unspecified), VietnamTimeZone);
    }

    public static DateTime DisplayDateStartUtc(DateOnly value)
        => DisplayTimeToUtc(value.ToDateTime(TimeOnly.MinValue));

    public static DateTime DisplayDateEndExclusiveUtc(DateOnly value)
        => DisplayDateStartUtc(value.AddDays(1));

    public static string DateTime(DateTime? value)
        => value.HasValue ? ToDisplayTime(value.Value).ToString("dd/MM/yyyy HH:mm", ViCulture) : "—";

    public static string DateTimeSeconds(DateTime? value)
        => value.HasValue ? ToDisplayTime(value.Value).ToString("dd/MM/yyyy HH:mm:ss", ViCulture) : "—";

    public static string ShortDateTime(DateTime? value)
        => value.HasValue ? ToDisplayTime(value.Value).ToString("dd/MM HH:mm", ViCulture) : "—";

    public static string Date(DateTime? value)
        => value.HasValue ? ToDisplayTime(value.Value).ToString("dd/MM/yyyy", ViCulture) : "—";

    public static string Time(DateTime? value)
        => value.HasValue ? ToDisplayTime(value.Value).ToString("HH:mm", ViCulture) : "—";

    public static string DateTimeLocalInput(DateTime? value)
        => value.HasValue ? ToDisplayTime(value.Value).ToString("yyyy-MM-ddTHH:mm", ViCulture) : string.Empty;

    public static string TodayLine()
    {
        var now = Now();
        return $"{ViDays[(int)now.DayOfWeek]}, ng\u00e0y {now:dd/MM/yyyy}";
    }

    private static TimeZoneInfo ResolveVietnamTimeZone()
    {
        foreach (var id in new[] { "SE Asia Standard Time", "Asia/Ho_Chi_Minh" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }

        return TimeZoneInfo.CreateCustomTimeZone(
            "Vietnam Standard Time",
            TimeSpan.FromHours(7),
            "Vietnam Standard Time",
            "Vietnam Standard Time");
    }
}
