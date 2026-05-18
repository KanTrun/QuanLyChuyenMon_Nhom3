using TelemedicineLandingPage.Models.Admin;

namespace TelemedicineLandingPage.Services.Admin;

/// <summary>
/// Singleton in-memory implementation of <see cref="IProcedureService"/>.
/// Raises StateChanged on every mutation so subscribed Razor components can
/// re-render.
/// </summary>
public sealed class ProcedureService : IProcedureService
{
    private readonly object _gate = new();
    private readonly List<ProcedureRecord> _items;

    public ProcedureService()
    {
        _items = SeedData();
    }

    public event Action? StateChanged;

    public IReadOnlyList<ProcedureRecord> Search(ProcedureFilter filter)
    {
        lock (_gate)
        {
            IEnumerable<ProcedureRecord> query = _items;

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var needle = filter.Search.Trim();
                query = query.Where(p =>
                    p.Name.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                    p.Code.Contains(needle, StringComparison.OrdinalIgnoreCase));
            }

            if (filter.Status is { } status)
            {
                query = query.Where(p => p.Status == status);
            }

            if (filter.Department is { } dept)
            {
                query = query.Where(p => p.Department == dept);
            }

            if (filter.FromDate is { } from)
            {
                query = query.Where(p => p.EffectiveFrom >= from);
            }

            if (filter.ToDate is { } to)
            {
                query = query.Where(p => p.EffectiveFrom <= to);
            }

            return query
                .OrderByDescending(p => p.UpdatedAt)
                .ToList();
        }
    }

    public ProcedureRecord? GetById(Guid id)
    {
        lock (_gate)
        {
            return _items.FirstOrDefault(p => p.Id == id);
        }
    }

    public ProcedureRecord Create(ProcedureRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var next = record with
        {
            Id = record.Id == Guid.Empty ? Guid.NewGuid() : record.Id,
            UpdatedAt = DateTime.Now,
        };
        lock (_gate)
        {
            _items.Add(next);
        }
        Raise();
        return next;
    }

    public ProcedureRecord Update(Guid id, ProcedureRecord updated)
    {
        ArgumentNullException.ThrowIfNull(updated);
        ProcedureRecord? next;
        lock (_gate)
        {
            var index = _items.FindIndex(p => p.Id == id);
            if (index < 0) throw new KeyNotFoundException($"Không tìm thấy quy trình với mã {id}.");
            next = updated with { Id = id, UpdatedAt = DateTime.Now };
            _items[index] = next;
        }
        Raise();
        return next;
    }

    public void Archive(Guid id, string actor)
    {
        Mutate(id, p => p with { Status = ProcedureStatus.NgungSuDung, UpdatedBy = actor });
    }

    public void SubmitForApproval(Guid id, string actor)
    {
        Mutate(id, p => p with { Status = ProcedureStatus.DangChoPheDuyet, UpdatedBy = actor });
    }

    public void Approve(Guid id, string approver)
    {
        Mutate(id, p => p with
        {
            Status = ProcedureStatus.DaBanHanh,
            UpdatedBy = approver,
            RejectionReason = null,
            EffectiveFrom = p.EffectiveFrom == default ? DateOnly.FromDateTime(DateTime.Today) : p.EffectiveFrom,
        });
    }

    public void Reject(Guid id, string approver, string reason)
    {
        Mutate(id, p => p with
        {
            Status = ProcedureStatus.DangSoanThao,
            UpdatedBy = approver,
            RejectionReason = reason,
        });
    }

    private void Mutate(Guid id, Func<ProcedureRecord, ProcedureRecord> mutator)
    {
        lock (_gate)
        {
            var index = _items.FindIndex(p => p.Id == id);
            if (index < 0) return;
            _items[index] = mutator(_items[index]) with { UpdatedAt = DateTime.Now };
        }
        Raise();
    }

    private void Raise() => StateChanged?.Invoke();

    private static List<ProcedureRecord> SeedData()
    {
        return new List<ProcedureRecord>();
    }
}
