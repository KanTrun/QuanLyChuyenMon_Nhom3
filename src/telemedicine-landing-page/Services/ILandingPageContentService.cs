using TelemedicineLandingPage.Models;

namespace TelemedicineLandingPage.Services;

public interface ILandingPageContentService
{
    IReadOnlyList<LandingStat> GetStats();

    IReadOnlyList<TrustSignal> GetTrustSignals();

    IReadOnlyList<SpecialistProfile> GetSpecialists();

    IReadOnlyList<HealthMetric> GetHealthMetrics();
}
