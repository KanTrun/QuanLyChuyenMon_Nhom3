using System.Globalization;

namespace TelemedicineLandingPage.Models.Admin;

/// <summary>
/// Formats UTC timestamps consistently for the Vietnamese admin UI.
/// </summary>
public static class AdminDateTimeDisplay
{
    private static readonly CultureInfo ViCulture = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly TimeZoneInfo VietnamTimeZone = ResolveVietnamTimeZone();

    public static DateTime ToDisplayTime(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Utc ? value : global::System.DateTime.SpecifyKind(value, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, VietnamTimeZone);
    }

    public static string DateTime(DateTime? value)
        => value.HasValue ? ToDisplayTime(value.Value).ToString("dd/MM/yyyy HH:mm", ViCulture) : "—";

    public static string DateTimeSeconds(DateTime? value)
        => value.HasValue ? ToDisplayTime(value.Value).ToString("dd/MM/yyyy HH:mm:ss", ViCulture) : "—";

    public static string ShortDateTime(DateTime? value)
        => value.HasValue ? ToDisplayTime(value.Value).ToString("dd/MM HH:mm", ViCulture) : "—";

    public static string Date(DateTime? value)
        => value.HasValue ? ToDisplayTime(value.Value).ToString("dd/MM/yyyy", ViCulture) : "—";

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

        return TimeZoneInfo.Local;
    }
}
