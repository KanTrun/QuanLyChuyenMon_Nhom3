namespace TelemedicineLandingPage.Models.Admin;

/// <summary>
/// Maps the QLCM Pro admin enums to human readable Vietnamese labels (with full
/// diacritics) and to the admin badge tone classes used by the data tables.
/// </summary>
public static class AdminEnumLabels
{
    public static string GetLabel(ProcedureStatus value) => value switch
    {
        ProcedureStatus.DangSoanThao => "Đang soạn thảo",
        ProcedureStatus.DangChoPheDuyet => "Đang chờ phê duyệt",
        ProcedureStatus.DaBanHanh => "Đã ban hành",
        ProcedureStatus.NgungSuDung => "Ngừng sử dụng",
        _ => value.ToString(),
    };

    public static string GetTone(ProcedureStatus value) => value switch
    {
        ProcedureStatus.DangSoanThao => "tone-muted",
        ProcedureStatus.DangChoPheDuyet => "tone-warning",
        ProcedureStatus.DaBanHanh => "tone-success",
        ProcedureStatus.NgungSuDung => "tone-danger",
        _ => "tone-muted",
    };

    public static string GetLabel(Department value) => value switch
    {
        Department.TimMach => "Tim mạch",
        Department.NoiTiet => "Nội tiết",
        Department.NhiKhoa => "Nhi khoa",
        Department.NgoaiTongQuat => "Ngoại tổng quát",
        Department.SanPhuKhoa => "Sản phụ khoa",
        Department.ChanDoanHinhAnh => "Chẩn đoán hình ảnh",
        Department.XetNghiem => "Xét nghiệm",
        Department.DuocLamSang => "Dược lâm sàng",
        Department.KhoVatTu => "Kho vật tư",
        Department.HanhChinh => "Hành chính",
        _ => value.ToString(),
    };

    public static string GetLabel(ResourceType value) => value switch
    {
        ResourceType.Thuoc => "Thuốc",
        ResourceType.VatTu => "Vật tư",
        ResourceType.ThietBi => "Thiết bị",
        ResourceType.HoaChat => "Hóa chất",
        _ => value.ToString(),
    };

    public static string GetLabel(ProtocolType value) => value switch
    {
        ProtocolType.ChamSoc => "Chăm sóc",
        ProtocolType.PhauThuat => "Phẫu thuật",
        ProtocolType.ThuThuat => "Thủ thuật",
        ProtocolType.DieuTri => "Điều trị",
        _ => value.ToString(),
    };

    public static string GetLabel(ServiceType value) => value switch
    {
        ServiceType.KyThuat => "Kỹ thuật",
        ServiceType.XetNghiem => "Xét nghiệm",
        ServiceType.ChanDoanHinhAnh => "Chẩn đoán hình ảnh",
        ServiceType.PhauThuat => "Phẫu thuật",
        ServiceType.ThuThuat => "Thủ thuật",
        _ => value.ToString(),
    };

    public static string GetLabel(CatalogStatus value) => value switch
    {
        CatalogStatus.HoatDong => "Hoạt động",
        CatalogStatus.TamNgung => "Tạm ngưng",
        CatalogStatus.NgungSuDung => "Ngừng sử dụng",
        _ => value.ToString(),
    };

    public static string GetTone(CatalogStatus value) => value switch
    {
        CatalogStatus.HoatDong => "tone-success",
        CatalogStatus.TamNgung => "tone-warning",
        CatalogStatus.NgungSuDung => "tone-danger",
        _ => "tone-muted",
    };

    public static string GetLabel(PermissionTargetType value) => value switch
    {
        PermissionTargetType.User => "Tài khoản",
        PermissionTargetType.Role => "Vai trò",
        _ => value.ToString(),
    };

    public static string GetLabel(ClinicSessionStatus value) => value switch
    {
        ClinicSessionStatus.DangCho => "Đang chờ",
        ClinicSessionStatus.DangThucHien => "Đang thực hiện",
        ClinicSessionStatus.HoanThanh => "Hoàn thành",
        _ => value.ToString(),
    };

    public static string GetTone(ClinicSessionStatus value) => value switch
    {
        ClinicSessionStatus.DangCho => "tone-muted",
        ClinicSessionStatus.DangThucHien => "tone-secondary",
        ClinicSessionStatus.HoanThanh => "tone-success",
        _ => "tone-muted",
    };

    public static string GetLabel(ActivitySeverity value) => value switch
    {
        ActivitySeverity.Info => "Thông tin",
        ActivitySeverity.Warning => "Cảnh báo",
        ActivitySeverity.Critical => "Quan trọng",
        _ => value.ToString(),
    };

    public static string GetTone(ActivitySeverity value) => value switch
    {
        ActivitySeverity.Info => "tone-secondary",
        ActivitySeverity.Warning => "tone-warning",
        ActivitySeverity.Critical => "tone-danger",
        _ => "tone-muted",
    };
}
