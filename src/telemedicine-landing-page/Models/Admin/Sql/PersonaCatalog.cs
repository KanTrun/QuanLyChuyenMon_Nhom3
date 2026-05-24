namespace TelemedicineLandingPage.Models.Admin.Sql;

/// <summary>Persona (vai trò nghiệp vụ) trong hệ thống QLCM.</summary>
public enum Persona
{
    /// <summary>Quản trị hệ thống — toàn quyền cấu hình.</summary>
    QuanTriHeThong,

    /// <summary>Quản trị khoa/phòng — quản lý trong phạm vi đơn vị.</summary>
    QuanTriKhoaPhong,

    /// <summary>Người phê duyệt quyền — xử lý yêu cầu thay đổi quyền.</summary>
    NguoiPheDuyet,

    /// <summary>Quản lý quy trình — soạn thảo và gửi duyệt quy trình.</summary>
    QuanLyQuyTrinh,

    /// <summary>Quản lý tài nguyên — định mức và theo dõi vật tư.</summary>
    QuanLyTaiNguyen,

    /// <summary>Điều phối kỹ thuật — tạo và theo dõi chỉ định.</summary>
    DieuPhoiKyThuat,

    /// <summary>Quản lý phác đồ — soạn thảo phác đồ lâm sàng.</summary>
    QuanLyPhacDo,

    /// <summary>Người dùng lâm sàng — bác sĩ, dược sĩ thực hiện chuyên môn.</summary>
    NguoiDungLamSang,

    /// <summary>Xem báo cáo — chỉ xem thống kê.</summary>
    XemBaoCao
}

/// <summary>Ánh xạ vai trò hệ thống sang persona nghiệp vụ.</summary>
public static class PersonaCatalog
{
    /// <summary>Danh sách persona với nhãn hiển thị và mã vai trò tương ứng.</summary>
    public static IReadOnlyList<PersonaInfo> All { get; } = new List<PersonaInfo>
    {
        new(Persona.QuanTriHeThong, "Quản trị hệ thống", "SYSTEM_ADMIN", "/admin", "shield"),
        new(Persona.QuanTriKhoaPhong, "Quản trị khoa/phòng", "DEPARTMENT_ADMIN", "/admin", "building"),
        new(Persona.NguoiPheDuyet, "Người phê duyệt", "SYSTEM_ADMIN", "/phe-duyet", "check-circle"),
        new(Persona.QuanLyQuyTrinh, "Quản lý quy trình", "DEPARTMENT_ADMIN", "/quy-trinh-pro", "workflow"),
        new(Persona.QuanLyTaiNguyen, "Quản lý tài nguyên", "DEPARTMENT_ADMIN", "/tai-nguyen", "package"),
        new(Persona.DieuPhoiKyThuat, "Điều phối kỹ thuật", "CLINICAL_USER", "/dieu-phoi", "clipboard"),
        new(Persona.QuanLyPhacDo, "Quản lý phác đồ", "CLINICAL_USER", "/phac-do-pro", "stethoscope"),
        new(Persona.NguoiDungLamSang, "Người dùng lâm sàng", "CLINICAL_USER", "/lam-sang", "heart"),
        new(Persona.XemBaoCao, "Xem báo cáo", "REPORT_VIEWER", "/qlcm/bao-cao", "chart"),
    };

    /// <summary>Lấy persona phù hợp nhất cho vai trò.</summary>
    public static IReadOnlyList<PersonaInfo> GetForRole(string roleCode)
    {
        return All.Where(p => p.RoleCode == roleCode).ToList();
    }

    /// <summary>Lấy persona mặc định cho người dùng dựa trên vai trò đầu tiên.</summary>
    public static PersonaInfo GetDefault(string? roleCode)
    {
        if (roleCode is not null)
        {
            var match = All.FirstOrDefault(p => p.RoleCode == roleCode);
            if (match is not null) return match;
        }
        return All[^1]; // Mặc định: Xem báo cáo
    }
}

/// <summary>Thông tin chi tiết của một persona.</summary>
public sealed record PersonaInfo(
    Persona Persona,
    string Label,
    string RoleCode,
    string DefaultRoute,
    string Icon);
