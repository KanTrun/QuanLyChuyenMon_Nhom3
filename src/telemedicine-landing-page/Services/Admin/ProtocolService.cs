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
            UpdatedAt = DateTime.Now,
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
            next = updated with { Id = id, UpdatedAt = DateTime.Now };
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
            _items[index] = _items[index] with { Status = CatalogStatus.NgungSuDung, UpdatedAt = DateTime.Now };
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
                UpdatedAt = DateTime.Now,
            };
            _items[index] = next;
            entry = new ProtocolApplication(
                Guid.NewGuid(),
                protocolId,
                patientName.Trim(),
                string.IsNullOrWhiteSpace(outcome) ? "Đang theo dõi" : outcome.Trim(),
                DateTime.Now);
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
        var today = DateOnly.FromDateTime(DateTime.Today);
        return new List<ClinicalProtocolRecord>
        {
            new()
            {
                Code = "PD-DTD2",
                Name = "Phác đồ điều trị tiểu đường tuýp 2",
                ProtocolType = ProtocolType.DieuTri,
                Specialty = Department.NoiTiet,
                IcdCodes = new[] { "E11", "E11.9" },
                Contraindications = "Suy thận giai đoạn cuối; dị ứng metformin.",
                Status = CatalogStatus.HoatDong,
                Version = "2.3",
                EffectiveFrom = today.AddMonths(-8),
                ApplicationCount = 142,
            },
            new()
            {
                Code = "PD-CSHP-NHI",
                Name = "Phác đồ chăm sóc hậu phẫu nhi",
                ProtocolType = ProtocolType.ChamSoc,
                Specialty = Department.NhiKhoa,
                IcdCodes = new[] { "Z48.8" },
                Contraindications = "Trẻ sinh non dưới 32 tuần thai (tham khảo hội chẩn).",
                Status = CatalogStatus.HoatDong,
                Version = "1.4",
                EffectiveFrom = today.AddMonths(-5),
                ApplicationCount = 58,
            },
            new()
            {
                Code = "PD-SPV",
                Name = "Phác đồ cấp cứu sốc phản vệ",
                ProtocolType = ProtocolType.DieuTri,
                Specialty = Department.NgoaiTongQuat,
                IcdCodes = new[] { "T78.2" },
                Contraindications = "Theo dõi sát BN có bệnh tim mạch nặng khi dùng adrenaline.",
                Status = CatalogStatus.HoatDong,
                Version = "3.0",
                EffectiveFrom = today.AddMonths(-12),
                ApplicationCount = 21,
            },
            new()
            {
                Code = "PD-THA",
                Name = "Phác đồ điều trị tăng huyết áp",
                ProtocolType = ProtocolType.DieuTri,
                Specialty = Department.TimMach,
                IcdCodes = new[] { "I10" },
                Contraindications = "Phụ nữ có thai dùng nhóm ƯCMC/ARB.",
                Status = CatalogStatus.HoatDong,
                Version = "2.0",
                EffectiveFrom = today.AddMonths(-10),
                ApplicationCount = 98,
            },
            new()
            {
                Code = "PD-DOT-QUY",
                Name = "Phác đồ chăm sóc bệnh nhân đột quỵ não",
                ProtocolType = ProtocolType.ChamSoc,
                Specialty = Department.TimMach,
                IcdCodes = new[] { "I63", "I64" },
                Contraindications = "Chảy máu não cấp chưa kiểm soát.",
                Status = CatalogStatus.HoatDong,
                Version = "1.2",
                EffectiveFrom = today.AddMonths(-4),
                ApplicationCount = 34,
            },
            new()
            {
                Code = "PD-VRT",
                Name = "Phác đồ phẫu thuật nội soi viêm ruột thừa",
                ProtocolType = ProtocolType.PhauThuat,
                Specialty = Department.NgoaiTongQuat,
                IcdCodes = new[] { "K35.8" },
                Contraindications = "Dính ổ bụng nặng; rối loạn đông máu chưa kiểm soát.",
                Status = CatalogStatus.HoatDong,
                Version = "2.1",
                EffectiveFrom = today.AddMonths(-7),
                ApplicationCount = 67,
            },
        };
    }
}
