namespace TelemedicineLandingPage.Models.Admin.Sql;

/// <summary>
/// Bản ghi tra cứu dùng chung cho tất cả bảng lookup trong hệ thống.
/// </summary>
public sealed record LookupEntry(
    string Code,
    string Name,
    int DisplayOrder = 0,
    bool IsActive = true,
    string? Description = null);

/// <summary>
/// Danh mục đơn vị tính với nhóm đơn vị.
/// </summary>
public sealed record UnitEntry(
    string Code,
    string Name,
    string? UnitGroup = null,
    int DisplayOrder = 0,
    bool IsActive = true,
    string? Description = null);

/// <summary>
/// Lớp tĩnh chứa tất cả dữ liệu tra cứu (lookup) từ cơ sở dữ liệu SQL.
/// Mỗi danh sách tương ứng với một bảng lookup_* hoặc unit_catalog.
/// </summary>
public static class MedLookups
{
    public static IReadOnlyList<LookupEntry> RecordStatuses { get; } = new[]
    {
        new LookupEntry("active", "Đang hoạt động"),
        new LookupEntry("inactive", "Ngừng hoạt động"),
        new LookupEntry("archived", "Lưu trữ"),
    };

    public static IReadOnlyList<LookupEntry> UserOnboardingStatuses { get; } = new[]
    {
        new LookupEntry("submitted", "Chờ duyệt"),
        new LookupEntry("active", "Đang hoạt động"),
        new LookupEntry("rejected", "Bị từ chối"),
        new LookupEntry("inactive", "Ngừng hoạt động"),
    };

    public static IReadOnlyList<LookupEntry> ActionCodes { get; } = new[]
    {
        new LookupEntry("view", "Xem"),
        new LookupEntry("create", "Tạo mới"),
        new LookupEntry("update", "Cập nhật"),
        new LookupEntry("delete", "Xóa"),
        new LookupEntry("approve", "Phê duyệt"),
        new LookupEntry("publish", "Ban hành"),
        new LookupEntry("execute", "Thực hiện"),
        new LookupEntry("sign", "Ký xác nhận"),
        new LookupEntry("export", "Xuất dữ liệu"),
        new LookupEntry("configure", "Cấu hình"),
    };

    public static IReadOnlyList<LookupEntry> DepartmentScopeTypes { get; } = new[]
    {
        new LookupEntry("global", "Toàn hệ thống"),
        new LookupEntry("department", "Một khoa/phòng"),
        new LookupEntry("department_tree", "Cây khoa/phòng"),
        new LookupEntry("own_department", "Khoa/phòng của người dùng"),
        new LookupEntry("custom", "Quy tắc tùy chỉnh"),
    };

    public static IReadOnlyList<LookupEntry> PermissionEffects { get; } = new[]
    {
        new LookupEntry("allow", "Cho phép"),
        new LookupEntry("deny", "Từ chối"),
    };

    public static IReadOnlyList<LookupEntry> VersionStatuses { get; } = new[]
    {
        new LookupEntry("draft", "Bản nháp"),
        new LookupEntry("pending_approval", "Chờ phê duyệt"),
        new LookupEntry("active", "Đang hiệu lực"),
        new LookupEntry("superseded", "Đã được thay thế"),
        new LookupEntry("archived", "Lưu trữ"),
        new LookupEntry("rejected", "Bị từ chối"),
    };

    public static IReadOnlyList<LookupEntry> EnforcementModes { get; } = new[]
    {
        new LookupEntry("off", "Tắt"),
        new LookupEntry("warning", "Cảnh báo"),
        new LookupEntry("block", "Chặn"),
    };

    public static IReadOnlyList<LookupEntry> ResourceTypes { get; } = new[]
    {
        new LookupEntry("supply", "Vật tư tiêu hao"),
        new LookupEntry("equipment", "Thiết bị"),
        new LookupEntry("drug", "Thuốc"),
        new LookupEntry("chemical", "Hóa chất"),
    };

    public static IReadOnlyList<LookupEntry> NotificationChannels { get; } = new[]
    {
        new LookupEntry("in_app", "Trong ứng dụng"),
        new LookupEntry("email", "Email"),
        new LookupEntry("sms", "SMS"),
        new LookupEntry("zalo", "Zalo"),
        new LookupEntry("webhook", "Webhook"),
    };

    public static IReadOnlyList<LookupEntry> PermissionChangeStatuses { get; } = new[]
    {
        new LookupEntry("draft", "Bản nháp"),
        new LookupEntry("pending_approval", "Chờ phê duyệt"),
        new LookupEntry("scheduled", "Đã lên lịch"),
        new LookupEntry("applied", "Đã áp dụng"),
        new LookupEntry("rejected", "Bị từ chối"),
        new LookupEntry("failed", "Thất bại"),
        new LookupEntry("cancelled", "Đã hủy"),
    };

    public static IReadOnlyList<LookupEntry> PermissionChangeOperations { get; } = new[]
    {
        new LookupEntry("grant", "Cấp quyền"),
        new LookupEntry("revoke", "Thu hồi quyền"),
        new LookupEntry("update", "Cập nhật quyền"),
    };

    public static IReadOnlyList<LookupEntry> ProcedureTypes { get; } = new[]
    {
        new LookupEntry("technical", "Kỹ thuật"),
        new LookupEntry("care", "Chăm sóc"),
        new LookupEntry("surgery", "Phẫu thuật"),
        new LookupEntry("procedure", "Thủ thuật"),
    };

    public static IReadOnlyList<LookupEntry> ServiceTypes { get; } = new[]
    {
        new LookupEntry("lab", "Xét nghiệm"),
        new LookupEntry("imaging", "Chẩn đoán hình ảnh"),
        new LookupEntry("procedure", "Thủ thuật"),
        new LookupEntry("surgery", "Phẫu thuật"),
        new LookupEntry("care", "Chăm sóc"),
        new LookupEntry("other", "Khác"),
    };

    public static IReadOnlyList<LookupEntry> ProtocolTypes { get; } = new[]
    {
        new LookupEntry("care", "Chăm sóc"),
        new LookupEntry("treatment_protocol", "Phác đồ điều trị"),
        new LookupEntry("surgery", "Phẫu thuật"),
        new LookupEntry("procedure", "Thủ thuật"),
    };

    public static IReadOnlyList<LookupEntry> AttachmentTypes { get; } = new[]
    {
        new LookupEntry("sop", "SOP"),
        new LookupEntry("guideline", "Hướng dẫn"),
        new LookupEntry("form", "Biểu mẫu"),
        new LookupEntry("reference", "Tham khảo"),
        new LookupEntry("other", "Khác"),
    };

    public static IReadOnlyList<LookupEntry> OrderStatuses { get; } = new[]
    {
        new LookupEntry("ordered", "Đã chỉ định"),
        new LookupEntry("resource_warning", "Cảnh báo nguồn lực"),
        new LookupEntry("scheduled", "Đã lên lịch"),
        new LookupEntry("in_progress", "Đang thực hiện"),
        new LookupEntry("completed", "Hoàn thành"),
        new LookupEntry("cancelled", "Đã hủy"),
    };

    public static IReadOnlyList<LookupEntry> AvailabilityStatuses { get; } = new[]
    {
        new LookupEntry("available", "Sẵn sàng"),
        new LookupEntry("insufficient", "Không đủ"),
        new LookupEntry("unknown", "Chưa xác định"),
        new LookupEntry("adapter_error", "Lỗi adapter"),
    };

    public static IReadOnlyList<LookupEntry> PermissionChangeTargetTypes { get; } = new[]
    {
        new LookupEntry("role", "Vai trò"),
        new LookupEntry("group", "Nhóm"),
        new LookupEntry("user", "Người dùng"),
    };

    public static IReadOnlyList<LookupEntry> ProtocolRelationTypes { get; } = new[]
    {
        new LookupEntry("references", "Tham chiếu"),
        new LookupEntry("requires", "Bắt buộc"),
        new LookupEntry("optional", "Tùy chọn"),
    };

    public static IReadOnlyList<LookupEntry> ProtocolApplicationStatuses { get; } = new[]
    {
        new LookupEntry("suggested", "Đề xuất"),
        new LookupEntry("draft", "Bản nháp"),
        new LookupEntry("applied", "Đã áp dụng"),
        new LookupEntry("signed", "Đã ký"),
        new LookupEntry("revoked", "Đã thu hồi"),
        new LookupEntry("skipped", "Bỏ qua"),
        new LookupEntry("cancelled", "Đã hủy"),
    };

    public static IReadOnlyList<LookupEntry> DeliveryStatuses { get; } = new[]
    {
        new LookupEntry("pending", "Chờ gửi"),
        new LookupEntry("sent", "Đã gửi"),
        new LookupEntry("failed", "Thất bại"),
        new LookupEntry("skipped", "Bỏ qua"),
    };

    public static IReadOnlyList<LookupEntry> NotificationSeverities { get; } = new[]
    {
        new LookupEntry("info", "Thông tin"),
        new LookupEntry("warning", "Cảnh báo"),
        new LookupEntry("critical", "Nghiêm trọng"),
    };

    public static IReadOnlyList<LookupEntry> Genders { get; } = new[]
    {
        new LookupEntry("male", "Nam"),
        new LookupEntry("female", "Nữ"),
        new LookupEntry("other", "Khác"),
        new LookupEntry("unknown", "Không xác định"),
    };

    public static IReadOnlyList<LookupEntry> ProtocolRuleTypes { get; } = new[]
    {
        new LookupEntry("icd", "Mã ICD"),
        new LookupEntry("patient_group", "Nhóm bệnh nhân"),
        new LookupEntry("department", "Khoa/phòng"),
        new LookupEntry("age", "Độ tuổi"),
        new LookupEntry("gender", "Giới tính"),
        new LookupEntry("condition", "Tình trạng"),
        new LookupEntry("contraindication", "Chống chỉ định"),
    };

    public static IReadOnlyList<UnitEntry> UnitCatalog { get; } = new[]
    {
        new UnitEntry("piece", "Cái", "count"),
        new UnitEntry("set", "Bộ", "count"),
        new UnitEntry("pair", "Đôi", "count"),
        new UnitEntry("box", "Hộp", "count"),
        new UnitEntry("pack", "Gói", "count"),
        new UnitEntry("roll", "Cuộn", "count"),
        new UnitEntry("bag", "Túi", "count"),
        new UnitEntry("bottle", "Chai", "count"),
        new UnitEntry("vial", "Lọ", "count"),
        new UnitEntry("ampoule", "Ống", "count"),
        new UnitEntry("tube", "Ống nghiệm", "count"),
        new UnitEntry("syringe", "Bơm tiêm", "count"),
        new UnitEntry("tablet", "Viên", "count"),
        new UnitEntry("capsule", "Viên nang", "count"),
        new UnitEntry("dose", "Liều", "count"),
        new UnitEntry("test", "Lần xét nghiệm", "count"),
        new UnitEntry("kit", "Bộ kit", "count"),
        new UnitEntry("strip", "Que thử", "count"),
        new UnitEntry("drop", "Giọt", "volume"),
        new UnitEntry("ml", "Millilít", "volume"),
        new UnitEntry("l", "Lít", "volume"),
        new UnitEntry("mcg", "Microgam", "mass"),
        new UnitEntry("mg", "Miligam", "mass"),
        new UnitEntry("g", "Gam", "mass"),
        new UnitEntry("kg", "Kilôgam", "mass"),
        new UnitEntry("iu", "Đơn vị quốc tế", "activity"),
        new UnitEntry("minute", "Phút", "time"),
        new UnitEntry("hour", "Giờ", "time"),
        new UnitEntry("day", "Ngày", "time"),
    };
}
