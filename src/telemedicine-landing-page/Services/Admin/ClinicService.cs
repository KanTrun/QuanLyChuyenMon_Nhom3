using TelemedicineLandingPage.Models.Admin;

namespace TelemedicineLandingPage.Services.Admin;

public sealed class ClinicService : IClinicService
{
    private readonly object _gate = new();
    private readonly List<ClinicSession> _sessions;

    public ClinicService()
    {
        _sessions = SeedData();
    }

    public event Action? StateChanged;

    public IReadOnlyList<ClinicSession> ListAll()
    {
        lock (_gate) return _sessions.OrderBy(s => s.ScheduledAt).ToList();
    }

    public IReadOnlyList<ClinicSession> ListByStatus(ClinicSessionStatus status)
    {
        lock (_gate) return _sessions.Where(s => s.Status == status).OrderBy(s => s.ScheduledAt).ToList();
    }

    public ClinicSession Move(Guid id, ClinicSessionStatus next)
    {
        ClinicSession updated;
        lock (_gate)
        {
            var index = _sessions.FindIndex(s => s.Id == id);
            if (index < 0) throw new KeyNotFoundException($"Không tìm thấy phiên lâm sàng {id}.");
            updated = _sessions[index] with { Status = next };
            _sessions[index] = updated;
        }
        Raise();
        return updated;
    }

    public ClinicSession Add(ClinicSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var next = session with
        {
            Id = session.Id == Guid.Empty ? Guid.NewGuid() : session.Id,
        };
        lock (_gate) _sessions.Add(next);
        Raise();
        return next;
    }

    private void Raise() => StateChanged?.Invoke();

    private static List<ClinicSession> SeedData()
    {
        return new List<ClinicSession>();
    }
}
