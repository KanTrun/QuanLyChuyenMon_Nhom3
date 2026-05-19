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
        var today = DateOnly.FromDateTime(DateTime.Today);
        return new List<ProcedureRecord>
        {
            new()
            {
                Code = "QT-NOI-001",
                Name = "Quy trình khám và theo dõi nội tiết",
                Department = Department.NoiTiet,
                Version = "1.0-draft",
                Status = ProcedureStatus.DangChoPheDuyet,
                EffectiveFrom = today.AddDays(7),
                UpdatedBy = "TS. Nguyễn Minh Khang",
                Steps = new[]
                {
                    new ProcedureStep(1, "Tiếp nhận", "Điều dưỡng", 5, "Đủ hồ sơ khám"),
                    new ProcedureStep(2, "Khám chuyên khoa", "Bác sĩ", 20, "Có chẩn đoán sơ bộ"),
                },
            },
            new()
            {
                Code = "QT-XN-001",
                Name = "Quy trình xét nghiệm công thức máu",
                Department = Department.XetNghiem,
                Version = "1.0",
                Status = ProcedureStatus.DaBanHanh,
                EffectiveFrom = today.AddDays(-30),
                UpdatedBy = "ThS. Phạm Thu Hà",
                Steps = new[]
                {
                    new ProcedureStep(1, "Nhận mẫu", "Kỹ thuật viên", 5, "Mẫu đạt tiêu chuẩn"),
                    new ProcedureStep(2, "Chạy máy", "Kỹ thuật viên", 15, "Máy QC hợp lệ"),
                    new ProcedureStep(3, "Trả kết quả", "Bác sĩ xét nghiệm", 5, "Kết quả đã kiểm tra"),
                },
            },
            new()
            {
                Code = "QT-NHI-VC-001",
                Name = "Quy trình tiêm vaccine cúm mùa",
                Department = Department.NhiKhoa,
                Version = "0.9",
                Status = ProcedureStatus.DangSoanThao,
                EffectiveFrom = today.AddDays(14),
                UpdatedBy = "BS. Nguyễn Nhật Linh",
                Steps = new[]
                {
                    new ProcedureStep(1, "Sàng lọc trước tiêm", "Bác sĩ", 10, "Không có chống chỉ định"),
                    new ProcedureStep(2, "Theo dõi sau tiêm", "Điều dưỡng", 30, "Không phản ứng bất lợi"),
                },
            },
        };
    }
}
