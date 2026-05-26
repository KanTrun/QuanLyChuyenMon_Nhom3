using TelemedicineLandingPage.Models.Admin;

namespace TelemedicineLandingPage.Services.Admin;

public sealed class ProtocolService : IProtocolService
{
    private readonly object _gate = new();
    private readonly List<ClinicalProtocolRecord> _items;
    private readonly List<ProtocolApplication> _applications = new();

    public ProtocolService()
    {
        _items = SeedData();
    }

    public event Action? StateChanged;

    public IReadOnlyList<ClinicalProtocolRecord> Search(string? query = null, ProtocolType? type = null, Department? specialty = null)
    {
        lock (_gate)
        {
            IEnumerable<ClinicalProtocolRecord> q = _items;
            if (!string.IsNullOrWhiteSpace(query))
            {
                var needle = query.Trim();
                q = q.Where(p => p.Name.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                                 p.Code.Contains(needle, StringComparison.OrdinalIgnoreCase));
            }
            if (type is { } t) q = q.Where(p => p.ProtocolType == t);
            if (specialty is { } s) q = q.Where(p => p.Specialty == s);
            return q.OrderBy(p => p.Code).ToList();
        }
    }

    public ClinicalProtocolRecord? GetById(Guid id)
    {
        lock (_gate) return _items.FirstOrDefault(p => p.Id == id);
    }

    public ClinicalProtocolRecord Create(ClinicalProtocolRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var next = record with
        {
            Id = record.Id == Guid.Empty ? Guid.NewGuid() : record.Id,
            UpdatedAt = DateTime.UtcNow,
        };
        lock (_gate) _items.Add(next);
        Raise();
        return next;
    }

    public ClinicalProtocolRecord Update(Guid id, ClinicalProtocolRecord updated)
    {
        ArgumentNullException.ThrowIfNull(updated);
        ClinicalProtocolRecord next;
        lock (_gate)
        {
            var index = _items.FindIndex(p => p.Id == id);
            if (index < 0) throw new KeyNotFoundException($"Không tìm thấy phác đồ {id}.");
            next = updated with { Id = id, UpdatedAt = DateTime.UtcNow };
            _items[index] = next;
        }
        Raise();
        return next;
    }

    public void Archive(Guid id)
    {
        lock (_gate)
        {
            var index = _items.FindIndex(p => p.Id == id);
            if (index < 0) return;
            _items[index] = _items[index] with { Status = CatalogStatus.NgungSuDung, UpdatedAt = DateTime.UtcNow };
        }
        Raise();
    }

    public ProtocolApplication RecordPatientApplication(Guid protocolId, string patientName, string outcome)
    {
        if (string.IsNullOrWhiteSpace(patientName)) throw new ArgumentException("Tên bệnh nhân không được để trống.", nameof(patientName));
        ProtocolApplication entry;
        lock (_gate)
        {
            var index = _items.FindIndex(p => p.Id == protocolId);
            if (index < 0) throw new KeyNotFoundException($"Không tìm thấy phác đồ {protocolId}.");
            var next = _items[index] with
            {
                ApplicationCount = _items[index].ApplicationCount + 1,
                UpdatedAt = DateTime.UtcNow,
            };
            _items[index] = next;
            entry = new ProtocolApplication(
                Guid.NewGuid(),
                protocolId,
                patientName.Trim(),
                string.IsNullOrWhiteSpace(outcome) ? "Đang theo dõi" : outcome.Trim(),
                DateTime.UtcNow);
            _applications.Add(entry);
        }
        Raise();
        return entry;
    }

    public IReadOnlyList<ProtocolApplication> GetApplications(Guid protocolId)
    {
        lock (_gate)
        {
            return _applications
                .Where(a => a.ProtocolId == protocolId)
                .OrderByDescending(a => a.AppliedAt)
                .ToList();
        }
    }

    private void Raise() => StateChanged?.Invoke();

    private static List<ClinicalProtocolRecord> SeedData()
    {
        return new List<ClinicalProtocolRecord>
        {
            // 1. Phác đồ điều trị tăng huyết áp
            new ClinicalProtocolRecord
            {
                Code = "PD-TIM-001",
                Name = "Phác đồ điều trị tăng huyết áp",
                ProtocolType = ProtocolType.DieuTri,
                Specialty = Department.TimMach,
                IcdCodes = new[] { "I10", "I11", "I12", "I13", "I15" },
                Contraindications = "Hạ huyết áp nặng, sốc tim, hẹp động mạch thận hai bên",
                Status = CatalogStatus.HoatDong,
                Version = "2.1",
                EffectiveFrom = new DateOnly(2024, 1, 1),
                EffectiveTo = null,
                ApplicationCount = 0,
            },
            // 2. Phác đồ điều trị đái tháo đường type 2
            new ClinicalProtocolRecord
            {
                Code = "PD-NOT-001",
                Name = "Phác đồ điều trị đái tháo đường type 2",
                ProtocolType = ProtocolType.DieuTri,
                Specialty = Department.NoiTiet,
                IcdCodes = new[] { "E11" },
                Contraindications = "Nhiễm toan ceton, suy thận nặng (eGFR < 15ml/phút)",
                Status = CatalogStatus.HoatDong,
                Version = "1.5",
                EffectiveFrom = new DateOnly(2024, 3, 15),
                EffectiveTo = null,
                ApplicationCount = 0,
            },
            // 3. Phác đồ xử trí sốt xuất huyết
            new ClinicalProtocolRecord
            {
                Code = "PD-NHI-001",
                Name = "Phác đồ xử trí sốt xuất huyết",
                ProtocolType = ProtocolType.DieuTri,
                Specialty = Department.NhiKhoa,
                IcdCodes = new[] { "A91" },
                Contraindications = "Không có chống chỉ định tuyệt đối, thận trọng khi có bệnh lý đông máu",
                Status = CatalogStatus.HoatDong,
                Version = "3.0",
                EffectiveFrom = new DateOnly(2024, 6, 1),
                EffectiveTo = null,
                ApplicationCount = 0,
            },
        };
    }
}
