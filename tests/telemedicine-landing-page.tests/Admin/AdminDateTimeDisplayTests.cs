using TelemedicineLandingPage.Models.Admin;

namespace TelemedicineLandingPage.Tests.Admin;

public sealed class AdminDateTimeDisplayTests
{
    [Fact]
    public void DateTime_FormatsUtcTimestampInVietnamTime()
    {
        var utc = new DateTime(2026, 5, 26, 3, 25, 0, DateTimeKind.Utc);

        var display = AdminDateTimeDisplay.DateTime(utc);

        Assert.Equal("26/05/2026 10:25", display);
    }

    [Fact]
    public void DisplayTimeToUtc_ConvertsVietnamInputToUtc()
    {
        var vietnamInput = new DateTime(2026, 5, 26, 10, 25, 0, DateTimeKind.Unspecified);

        var utc = AdminDateTimeDisplay.DisplayTimeToUtc(vietnamInput);

        Assert.Equal(DateTimeKind.Utc, utc.Kind);
        Assert.Equal(new DateTime(2026, 5, 26, 3, 25, 0, DateTimeKind.Utc), utc);
    }

    [Fact]
    public void DateTimeLocalInput_UsesVietnamWallClockValue()
    {
        var utc = new DateTime(2026, 5, 26, 3, 25, 0, DateTimeKind.Utc);

        var input = AdminDateTimeDisplay.DateTimeLocalInput(utc);

        Assert.Equal("2026-05-26T10:25", input);
    }

    [Fact]
    public void DisplayDateStartUtc_UsesVietnamMidnight()
    {
        var start = AdminDateTimeDisplay.DisplayDateStartUtc(new DateOnly(2026, 5, 26));

        Assert.Equal(new DateTime(2026, 5, 25, 17, 0, 0, DateTimeKind.Utc), start);
    }
}
