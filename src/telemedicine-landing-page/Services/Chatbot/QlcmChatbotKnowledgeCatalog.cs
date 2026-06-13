using System.Globalization;
using System.Text;

namespace TelemedicineLandingPage.Services.Chatbot;

public sealed record QlcmChatbotKnowledgeTopic(
    string Code,
    string[] Keywords,
    string Context,
    string DemoReply);

public static class QlcmChatbotKnowledgeCatalog
{
    public const string CorePrompt = """
        Bạn là trợ lý vận hành nội bộ của QLCM Pro. Chỉ hỗ trợ cách sử dụng phần mềm quản lý chuyên môn bệnh viện.
        Trả lời ngắn gọn, chính xác bằng tiếng Việt có dấu. Ưu tiên hướng dẫn thao tác theo route người dùng được phép mở.

        Quy tắc bắt buộc:
        - Không chẩn đoán, kê đơn, tư vấn điều trị hoặc đưa ra quyết định lâm sàng.
        - Không yêu cầu, suy đoán hoặc lặp lại thông tin định danh bệnh nhân.
        - Không khẳng định có tích hợp HIS, EMR, kho, dược hoặc thiết bị bên ngoài; hiện chỉ có service boundary nội bộ.
        - Chu ky ho so chi su dung xac nhan noi bo bang tai khoan dang nhap, thoi diem, anh chu ky ve tay neu co va audit/hash noi dung.
        - Hành động từ chat chỉ được điều hướng hoặc tạo bản nháp một lần trong sessionStorage; không tự ghi SQL.
        - Phân biệt API draft trong tài liệu kiến trúc với runtime Blazor đang triển khai.
        - Nếu thiếu dữ liệu hoặc người dùng không có route phù hợp, nói rõ giới hạn và hướng dẫn kiểm tra trong ứng dụng.
        """;

    public static IReadOnlyList<QlcmChatbotKnowledgeTopic> Topics { get; } =
    [
        new(
            "account-onboarding",
            ["dang ky", "tai khoan", "onboarding", "kich hoat", "tu choi"],
            "Tài khoản công khai đăng ký ở trạng thái chờ duyệt. Quản trị viên duyệt, từ chối hoặc gửi lại yêu cầu trước khi đăng nhập.",
            "Luồng tài khoản: `Đăng ký` -> quản trị viên mở `Tổ chức > Người dùng` -> duyệt hoặc từ chối -> tài khoản được kích hoạt mới đăng nhập được."),
        new(
            "permissions",
            ["phan quyen", "vai tro", "nhom", "quyen", "phe duyet quyen"],
            "Quyền hiệu lực được giải quyết từ vai trò, nhóm và ghi đè người dùng theo khoa/phòng. Priority cao hơn thắng; cùng priority thì deny thắng. Thay đổi quyền có luồng nháp, gửi duyệt, áp dụng ngay hoặc theo lịch.",
            "Phân quyền gồm vai trò, nhóm và ghi đè cá nhân theo khoa/phòng. Thay đổi cần lý do, gửi duyệt, sau đó áp dụng ngay hoặc lên lịch; hệ thống lưu audit và gửi thông báo."),
        new(
            "procedures",
            ["quy trinh", "sop", "buoc", "ban hanh", "version", "phien ban"],
            "Quy trình kỹ thuật có bản nháp, bước tuần tự, role phụ trách, SLA, mapping màn hình và chế độ warn/block. Phiên bản đi từ draft -> pending_approval -> active; bản active cũ thành superseded.",
            "Quy trình kỹ thuật: tạo bản nháp -> khai báo bước, vai trò, định mức và màn hình liên kết -> `Phê duyệt` -> ban hành. Runtime guard có thể cảnh báo hoặc chặn thao tác lệch quy trình."),
        new(
            "resources-orders",
            ["tai nguyen", "dinh muc", "vat tu", "thuoc", "thiet bi", "chi dinh", "dieu phoi", "ton kho"],
            "Danh mục kỹ thuật liên kết định mức vật tư, thuốc, hóa chất và thiết bị. Khi tạo chỉ định, hệ thống tạo snapshot nguồn lực nội bộ; chưa có API kho/dược thật. Chỉ định đi ordered -> scheduled -> in_progress -> completed hoặc cancelled.",
            "Tài nguyên và chỉ định: cấu hình định mức -> tạo chỉ định -> kiểm tra snapshot nguồn lực -> điều phối thực hiện -> ghi tiêu hao thực tế -> xem báo cáo chênh lệch. Snapshot hiện là dữ liệu nội bộ, chưa phải tồn kho kho/dược thật."),
        new(
            "protocols",
            ["phac do", "icd", "lam sang", "chong chi dinh", "ap dung"],
            "Phác đồ có version và rule ICD, tuổi, giới, khoa, chống chỉ định. Hệ thống chỉ gợi ý phác đồ active phù hợp; người dùng chuyên môn quyết định áp dụng.",
            "Phác đồ: tạo bản nháp -> khai báo rule áp dụng/chống chỉ định -> gửi duyệt và ban hành -> tra cứu gợi ý theo ICD -> người dùng chuyên môn chọn áp dụng. Trợ lý không thay thế quyết định lâm sàng."),
        new(
            "signatures",
            ["chu ky", "ky xac nhan", "thu hoi ky", "signature"],
            "Ap dung phac do co the chuyen applied -> signed -> revoked. Chu ky la xac nhan noi bo bang tai khoan dang nhap, thoi diem, metadata va hash noi dung; khong goi nha cung cap ben ngoai. Thu hoi bat buoc nhap ly do va luu audit.",
            "Chu ky ho so: mo `Lam sang` -> ho so trang thai `Da ap dung` -> `Ky` -> ve chu ky noi bo -> xac nhan. He thong gan chu ky voi tai khoan, thoi diem va hash noi dung; thu hoi chu ky phai nhap ly do va luu audit."),
        new(
            "audit-reports-notifications",
            ["bao cao", "audit", "nhat ky", "thong bao", "signalr", "tieu thu"],
            "Audit log là append-only. Báo cáo có tổng hợp và tiêu thụ so với định mức. Thông báo được lưu SQL và fan-out realtime qua SignalR.",
            "Bạn có thể dùng `Báo cáo` để xem tổng hợp và tiêu thụ, `Nhật ký` để truy vết thay đổi, `Thông báo` để đọc cập nhật realtime đã lưu trong SQL."),
        new(
            "settings",
            ["cai dat", "gemini", "chatbot", "tro ly", "api key", "giao dien"],
            "Cài đặt AI chỉ thay đổi model tương thích provider và phần tùy chỉnh giọng trả lời. Core knowledge và quy tắc an toàn luôn bắt buộc. API key phải nạp từ environment hoặc user-secrets.",
            "Trong `Cài đặt > Trợ lý AI`, bạn có thể chọn model tương thích và thêm hướng dẫn trả lời. API key phải cấu hình bằng environment hoặc `dotnet user-secrets`, không ghi vào source.")
    ];

    public static IReadOnlyList<QlcmChatbotKnowledgeTopic> FindRelevant(string? query, int limit = 3)
    {
        var normalized = Normalize(query);
        if (normalized.Length == 0) return Array.Empty<QlcmChatbotKnowledgeTopic>();

        return Topics
            .Select(topic => new
            {
                Topic = topic,
                Score = topic.Keywords.Count(keyword => normalized.Contains(Normalize(keyword), StringComparison.Ordinal))
            })
            .Where(match => match.Score > 0)
            .OrderByDescending(match => match.Score)
            .ThenBy(match => match.Topic.Code)
            .Take(Math.Max(1, limit))
            .Select(match => match.Topic)
            .ToList();
    }

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                if (character is '\u0111' or '\u0110')
                {
                    builder.Append('d');
                }
                else
                {
                    builder.Append(character);
                }
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
