using TelemedicineLandingPage.Models.Admin;

namespace TelemedicineLandingPage.Services.Admin;

/// <summary>
/// Singleton in-memory implementation of <see cref="IProcedureService"/>. Seeds
/// realistic Vietnamese sample data on first construction so the UI is never
/// empty, and raises StateChanged on every mutation so subscribed Razor
/// components can re-render.
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
        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(DateTime.Today);

        return new List<ProcedureRecord>
        {
            new()
            {
                Code = "QT-VAC-COVID",
                Name = "Quy trình tiêm vaccine COVID-19",
                Department = Department.NoiTiet,
                Version = "2.1",
                Status = ProcedureStatus.DaBanHanh,
                EffectiveFrom = today.AddMonths(-6),
                UpdatedBy = "BS. Nguyễn Minh An",
                UpdatedAt = now.AddHours(-3),
                Steps = new List<ProcedureStep>
                {
                    new(1, "Tiếp nhận và sàng lọc bệnh nhân", "Điều dưỡng", 5, "Hoàn tất phiếu sàng lọc"),
                    new(2, "Chuẩn bị vaccine và vật tư", "Điều dưỡng", 4, "Vaccine còn hạn sử dụng"),
                    new(3, "Tiêm và theo dõi 30 phút", "Bác sĩ", 35, "Không có phản ứng phụ"),
                    new(4, "Cập nhật sổ tiêm và xuất giấy chứng nhận", "Điều dưỡng", 6, "In giấy chứng nhận"),
                },
            },
            new()
            {
                Code = "QT-XN-MAU",
                Name = "Quy trình lấy máu xét nghiệm",
                Department = Department.XetNghiem,
                Version = "1.4",
                Status = ProcedureStatus.DaBanHanh,
                EffectiveFrom = today.AddMonths(-9),
                UpdatedBy = "ThS. Trần Phương Linh",
                UpdatedAt = now.AddDays(-1),
                Steps = new List<ProcedureStep>
                {
                    new(1, "Đối chiếu chỉ định và xác nhận thông tin bệnh nhân", "Điều dưỡng", 3, "Khớp ID và yêu cầu"),
                    new(2, "Sát khuẩn và chuẩn bị dụng cụ", "Điều dưỡng", 4, "Cồn 70 độ + bông vô khuẩn"),
                    new(3, "Lấy mẫu máu theo quy chuẩn", "Kỹ thuật viên", 5, "Đủ số lượng ống"),
                    new(4, "Dán nhãn và bàn giao mẫu", "Điều dưỡng", 3, "Nhãn rõ thông tin"),
                    new(5, "Cập nhật hệ thống LIS", "Kỹ thuật viên", 2, "Cập nhật trạng thái mẫu"),
                },
            },
            new()
            {
                Code = "QT-NS-DD",
                Name = "Quy trình nội soi dạ dày",
                Department = Department.NgoaiTongQuat,
                Version = "3.0",
                Status = ProcedureStatus.DangChoPheDuyet,
                EffectiveFrom = today.AddDays(15),
                UpdatedBy = "BS. Phạm Thanh Hải",
                UpdatedAt = now.AddHours(-1),
                Steps = new List<ProcedureStep>
                {
                    new(1, "Khám tiền nội soi và giải thích", "Bác sĩ", 10, "Bệnh nhân ký cam kết"),
                    new(2, "Gây mê tĩnh mạch nông", "Bác sĩ gây mê", 8, "BN ổn định sinh hiệu"),
                    new(3, "Tiến hành nội soi", "Bác sĩ", 25, "Khảo sát toàn bộ dạ dày"),
                    new(4, "Theo dõi hồi tỉnh 30 phút", "Điều dưỡng", 30, "BN tỉnh và ổn định"),
                    new(5, "Trả kết quả và tư vấn", "Bác sĩ", 12, "Xuất phiếu kết quả"),
                },
            },
            new()
            {
                Code = "QT-HSCC",
                Name = "Quy trình hồi sức cấp cứu",
                Department = Department.NgoaiTongQuat,
                Version = "4.2",
                Status = ProcedureStatus.DaBanHanh,
                EffectiveFrom = today.AddYears(-1),
                UpdatedBy = "BS. Hoàng Bảo Châu",
                UpdatedAt = now.AddDays(-3),
                Steps = new List<ProcedureStep>
                {
                    new(1, "Đánh giá A-B-C-D", "Bác sĩ", 3, "Đường thở thông"),
                    new(2, "Thiết lập đường truyền và oxy", "Điều dưỡng", 5, "Đặt được đường truyền"),
                    new(3, "Thuốc và can thiệp theo phác đồ", "Bác sĩ", 20, "BN đáp ứng tốt"),
                    new(4, "Chuyển ICU theo dõi", "Điều dưỡng", 10, "Bàn giao đầy đủ hồ sơ"),
                },
            },
            new()
            {
                Code = "QT-CP-THUOC",
                Name = "Quy trình cấp phát thuốc nội trú",
                Department = Department.DuocLamSang,
                Version = "1.2",
                Status = ProcedureStatus.DangSoanThao,
                EffectiveFrom = today.AddMonths(1),
                UpdatedBy = "DS. Đỗ Thanh Tùng",
                UpdatedAt = now.AddHours(-6),
                Steps = new List<ProcedureStep>
                {
                    new(1, "Tiếp nhận chỉ định từ HIS", "Dược sĩ", 4, "Khớp đơn nội trú"),
                    new(2, "Soạn thuốc theo từng giường", "Dược sĩ", 12, "Đối chiếu liều dùng"),
                    new(3, "Bàn giao điều dưỡng phụ trách", "Dược sĩ", 5, "Ký nhận đầy đủ"),
                    new(4, "Điều dưỡng cấp phát cho người bệnh", "Điều dưỡng", 8, "Có giám sát uống thuốc"),
                },
            },
            new()
            {
                Code = "QT-KSNK-PMO",
                Name = "Quy trình kiểm soát nhiễm khuẩn phòng mổ",
                Department = Department.NgoaiTongQuat,
                Version = "2.0",
                Status = ProcedureStatus.DaBanHanh,
                EffectiveFrom = today.AddMonths(-3),
                UpdatedBy = "ĐD. Mai Thị Lan",
                UpdatedAt = now.AddDays(-2),
                Steps = new List<ProcedureStep>
                {
                    new(1, "Vệ sinh phòng mổ trước ca", "Điều dưỡng", 20, "Đạt chuẩn ATP"),
                    new(2, "Khử khuẩn dụng cụ", "Kỹ thuật viên", 30, "Tiệt trùng đạt chuẩn"),
                    new(3, "Trang phục và sát khuẩn ekip", "Bác sĩ", 8, "Đầy đủ PPE"),
                    new(4, "Giám sát trong ca và sau ca", "Điều dưỡng", 15, "Không có sự cố"),
                    new(5, "Xử lý chất thải y tế", "Hộ lý", 12, "Phân loại đúng quy định"),
                },
            },
            new()
            {
                Code = "QT-CS-HAU-PHAU",
                Name = "Quy trình chăm sóc hậu phẫu",
                Department = Department.NgoaiTongQuat,
                Version = "1.5",
                Status = ProcedureStatus.DangChoPheDuyet,
                EffectiveFrom = today.AddDays(7),
                UpdatedBy = "ĐD. Vũ Hồng Hạnh",
                UpdatedAt = now.AddMinutes(-30),
                Steps = new List<ProcedureStep>
                {
                    new(1, "Đánh giá sinh hiệu mỗi 15 phút", "Điều dưỡng", 60, "Sinh hiệu ổn định"),
                    new(2, "Theo dõi vết mổ và dẫn lưu", "Điều dưỡng", 10, "Không chảy máu bất thường"),
                    new(3, "Quản lý đau theo bậc thang", "Bác sĩ", 6, "VAS dưới 4"),
                    new(4, "Hướng dẫn vận động sớm", "Điều dưỡng", 8, "BN hợp tác vận động"),
                },
            },
            new()
            {
                Code = "QT-TN-CC",
                Name = "Quy trình tiếp nhận bệnh nhân cấp cứu",
                Department = Department.HanhChinh,
                Version = "2.4",
                Status = ProcedureStatus.DaBanHanh,
                EffectiveFrom = today.AddMonths(-12),
                UpdatedBy = "BS. Đặng Thái Sơn",
                UpdatedAt = now.AddHours(-12),
                Steps = new List<ProcedureStep>
                {
                    new(1, "Phân loại theo thang điểm Triage", "Điều dưỡng", 4, "Phân loại trong 5 phút"),
                    new(2, "Hồ sơ tiếp nhận và bảo hiểm", "Hành chính", 6, "Khớp dữ liệu BHYT"),
                    new(3, "Khám và chỉ định ban đầu", "Bác sĩ", 10, "Có kế hoạch điều trị"),
                    new(4, "Chuyển khoa theo chỉ định", "Điều dưỡng", 5, "Bàn giao đầy đủ"),
                },
            },
        };
    }
}
