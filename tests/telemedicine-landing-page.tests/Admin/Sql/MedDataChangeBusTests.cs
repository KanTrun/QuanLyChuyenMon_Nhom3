using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Tests.Admin.Sql;

public sealed class MedDataChangeBusTests
{
    [Fact]
    public void Publish_IncrementsRevisionAndRaisesChanged()
    {
        var bus = new MedDataChangeBus();
        var raised = 0;
        bus.Changed += () => raised++;

        bus.Publish();
        bus.Publish();

        Assert.Equal(2, raised);
        Assert.Equal(2, bus.Revision);
    }
}
