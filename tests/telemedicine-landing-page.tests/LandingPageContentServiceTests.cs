using TelemedicineLandingPage.Services;

namespace TelemedicineLandingPage.Tests;

public sealed class LandingPageContentServiceTests
{
    private readonly LandingPageContentService service = new();

    [Fact]
    public void GetSpecialists_ReturnsVietnameseSpecialistDirectory()
    {
        var specialists = service.GetSpecialists();

        Assert.Equal(3, specialists.Count);
        Assert.Contains(specialists, item => item.Name.Contains("Nguyễn"));
        Assert.All(specialists, item => Assert.False(string.IsNullOrWhiteSpace(item.Availability)));
    }

    [Fact]
    public void GetHealthMetrics_ReturnsValidProgressValues()
    {
        var metrics = service.GetHealthMetrics();

        Assert.NotEmpty(metrics);
        Assert.All(metrics, item => Assert.InRange(item.ProgressPercent, 0, 100));
        Assert.Contains(metrics, item => item.Label == "Đường huyết");
    }
}
