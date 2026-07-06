using TelemedicineLandingPage.Services.Admin;

namespace TelemedicineLandingPage.Tests.Admin;

public sealed class ProtocolServiceTests
{
    [Fact]
    public void RecordPatientApplication_IncrementsCount()
    {
        var service = new ProtocolService();
        var protocol = service.Search().First();
        var initial = protocol.ApplicationCount;

        var raised = false;
        service.StateChanged += () => raised = true;

        var entry = service.RecordPatientApplication(protocol.Id, "Trần Hữu Bình", "Đáp ứng tốt sau 24 giờ");

        Assert.True(raised);
        Assert.Equal("Trần Hữu Bình", entry.PatientName);
        Assert.Equal("Đáp ứng tốt sau 24 giờ", entry.Outcome);

        var refreshed = service.GetById(protocol.Id);
        Assert.NotNull(refreshed);
        Assert.Equal(initial + 1, refreshed!.ApplicationCount);

        var history = service.GetApplications(protocol.Id);
        Assert.Single(history);
        Assert.Equal("Trần Hữu Bình", history[0].PatientName);
    }
}
