using TelemedicineLandingPage.Models;

namespace TelemedicineLandingPage.Tests;

public sealed class LandingPageLinksOptionsTests
{
    [Fact]
    public void HasValidUrls_AcceptsAnchorsAndAbsoluteUrls()
    {
        var options = new LandingPageLinksOptions
        {
            StartVisitUrl = "#consultation",
            FindSpecialistUrl = "#specialists",
            AppStoreUrl = "https://www.apple.com/app-store/",
            GooglePlayUrl = "https://play.google.com/store",
            PrivacyUrl = "#privacy",
            ContactUrl = "mailto:telehealth@benhvien.local"
        };

        Assert.True(options.HasValidUrls());
    }

    [Fact]
    public void HasValidUrls_RejectsBlankDestination()
    {
        var options = new LandingPageLinksOptions
        {
            ContactUrl = ""
        };

        Assert.False(options.HasValidUrls());
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==")]
    [InlineData("#")]
    [InlineData("#bad anchor")]
    public void HasValidUrls_RejectsUnsafeDestinations(string unsafeUrl)
    {
        var options = new LandingPageLinksOptions
        {
            StartVisitUrl = unsafeUrl
        };

        Assert.False(options.HasValidUrls());
    }
}
