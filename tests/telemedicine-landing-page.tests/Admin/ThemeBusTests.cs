using TelemedicineLandingPage.Services.Admin;

namespace TelemedicineLandingPage.Tests.Admin;

public sealed class ThemeBusTests
{
    [Theory]
    [InlineData("light", "light")]
    [InlineData("dark", "dark")]
    [InlineData("invalid", "light")]
    public void SetTheme_RaisesNormalizedTheme(string requested, string expected)
    {
        var bus = new ThemeBus();
        string? raised = null;

        bus.ThemeChanged += theme => raised = theme;
        bus.SetTheme(requested);

        Assert.Equal(expected, raised);
    }

    [Fact]
    public void SetMotion_RaisesRequestedValue()
    {
        var bus = new ThemeBus();
        bool? raised = null;

        bus.MotionChanged += enabled => raised = enabled;
        bus.SetMotion(false);

        Assert.False(raised);
    }
}
