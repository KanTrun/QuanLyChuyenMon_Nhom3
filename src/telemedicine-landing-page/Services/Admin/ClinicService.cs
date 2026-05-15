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
        TimeOnly Time(int h, int m) => new(h, m);
        return new List<ClinicSession>
        {
            new() { PatientCode = "BN-001", PatientName = "Nguyễn Thị Hồng", Department = Department.NoiTiet, TechnicalService = "Tiêm bắp Vitamin tổng hợp", AssignedTo = "ĐD. Mai Thị Lan", ScheduledAt = Time(8, 0), Status = ClinicSessionStatus.DangCho, Note = "Bệnh nhân đã ăn sáng" },
            new() { PatientCode = "BN-002", PatientName = "Trần Văn Bảo", Department = Department.NgoaiTongQuat, TechnicalService = "Thay băng vết thương", AssignedTo = "ĐD. Vũ Hồng Hạnh", ScheduledAt = Time(8, 15), Status = ClinicSessionStatus.DangCho, Note = "Vết mổ ngày thứ 3" },
            new() { PatientCode = "BN-003", PatientName = "Lê Hoàng Phúc", Department = Department.TimMach, TechnicalService = "Khám tim mạch cơ bản", AssignedTo = "BS. Lê Quang Huy", ScheduledAt = Time(8, 30), Status = ClinicSessionStatus.DangThucHien },
            new() { PatientCode = "BN-004", PatientName = "Phạm Thu Trang", Department = Department.SanPhuKhoa, TechnicalService = "Siêu âm thai 3 tháng cuối", AssignedTo = "BS. Hoàng Bảo Châu", ScheduledAt = Time(9, 0), Status = ClinicSessionStatus.DangThucHien },
            new() { PatientCode = "BN-005", PatientName = "Đặng Quốc Khánh", Department = Department.XetNghiem, TechnicalService = "Xét nghiệm công thức máu", AssignedTo = "KTV. Phan Hà Linh", ScheduledAt = Time(9, 15), Status = ClinicSessionStatus.HoanThanh },
            new() { PatientCode = "BN-006", PatientName = "Hoàng Mai Anh", Department = Department.NhiKhoa, TechnicalService = "Tiêm vaccine sởi", AssignedTo = "ĐD. Mai Thị Lan", ScheduledAt = Time(9, 30), Status = ClinicSessionStatus.DangCho, Note = "Trẻ 12 tháng tuổi" },
            new() { PatientCode = "BN-007", PatientName = "Vũ Đức Long", Department = Department.NgoaiTongQuat, TechnicalService = "Thủ thuật đặt sonde tiểu", AssignedTo = "ĐD. Vũ Hồng Hạnh", ScheduledAt = Time(10, 0), Status = ClinicSessionStatus.DangThucHien },
            new() { PatientCode = "BN-008", PatientName = "Bùi Thị Lan", Department = Department.ChanDoanHinhAnh, TechnicalService = "Chụp X-quang ngực thẳng", AssignedTo = "KTV. Trần Đức Mạnh", ScheduledAt = Time(10, 15), Status = ClinicSessionStatus.HoanThanh },
            new() { PatientCode = "BN-009", PatientName = "Đỗ Khắc Tuấn", Department = Department.NoiTiet, TechnicalService = "Đo huyết áp tự động", AssignedTo = "ĐD. Mai Thị Lan", ScheduledAt = Time(10, 30), Status = ClinicSessionStatus.HoanThanh },
            new() { PatientCode = "BN-010", PatientName = "Nguyễn Phương Anh", Department = Department.DuocLamSang, TechnicalService = "Cấp phát thuốc nội trú", AssignedTo = "DS. Đỗ Thanh Tùng", ScheduledAt = Time(10, 45), Status = ClinicSessionStatus.DangCho, Note = "Đợi đối chiếu chỉ định" },
        };
    }
}
