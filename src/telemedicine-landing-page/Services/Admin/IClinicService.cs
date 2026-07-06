using TelemedicineLandingPage.Models.Admin;

namespace TelemedicineLandingPage.Services.Admin;

/// <summary>In-memory clinical workboard service used by the Lâm sàng page.</summary>
public interface IClinicService
{
    IReadOnlyList<ClinicSession> ListAll();
    IReadOnlyList<ClinicSession> ListByStatus(ClinicSessionStatus status);
    ClinicSession Move(Guid id, ClinicSessionStatus next);
    ClinicSession Add(ClinicSession session);

    event Action? StateChanged;
}
