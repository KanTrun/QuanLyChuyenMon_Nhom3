using TelemedicineLandingPage.Models;

namespace TelemedicineLandingPage.Services;

public sealed class LandingPageContentService : ILandingPageContentService
{
    private static readonly IReadOnlyList<LandingStat> Stats =
    [
        new("12 phút", "thời gian kết nối trung bình"),
        new("38+", "chuyên khoa sẵn sàng"),
        new("24/7", "tiếp nhận yêu cầu hỗ trợ")
    ];

    private static readonly IReadOnlyList<TrustSignal> TrustSignals =
    [
        new("Bảo mật", "Dữ liệu được bảo vệ", "Thiết kế ưu tiên quyền riêng tư và kiểm soát truy cập"),
        new("4.8/5", "Hài lòng người bệnh", "Đánh giá từ các lần tái khám trực tuyến"),
        new("98.6%", "Hồ sơ đồng bộ", "Kết quả, đơn thuốc và lịch hẹn cập nhật liên tục")
    ];

    private static readonly IReadOnlyList<SpecialistProfile> Specialists =
    [
        new("BS. Nguyễn Minh An", "Tim mạch", "Trung tâm tim mạch", "Còn lịch 09:40", "Việt / Anh", "tone-sky"),
        new("ThS. Trần Phương Linh", "Nội tiết", "Phòng khám tiểu đường", "Còn lịch 11:20", "Việt", "tone-mint"),
        new("BS. Lê Quang Huy", "Nhi khoa", "Đơn vị chăm sóc trẻ em", "Còn lịch 14:10", "Việt / Pháp", "tone-coral")
    ];

    private static readonly IReadOnlyList<HealthMetric> HealthMetrics =
    [
        new("Huyết áp", "118/76", "ổn định 7 ngày", 78, "tone-sky"),
        new("Đường huyết", "5.8", "mmol/L trước ăn", 64, "tone-mint"),
        new("Giấc ngủ", "7h20", "theo dõi tự động", 72, "tone-blue")
    ];

    public IReadOnlyList<LandingStat> GetStats() => Stats;

    public IReadOnlyList<TrustSignal> GetTrustSignals() => TrustSignals;

    public IReadOnlyList<SpecialistProfile> GetSpecialists() => Specialists;

    public IReadOnlyList<HealthMetric> GetHealthMetrics() => HealthMetrics;
}
