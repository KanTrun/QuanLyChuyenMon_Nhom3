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
        Bạn là trợ lý vận hành nội bộ của QLCM Pro, chỉ hỗ trợ cách sử dụng phần mềm quản lý chuyên môn bệnh viện.
        Trả lời ngắn gọn, chính xác bằng tiếng Việt có dấu, bám sát nghiệp vụ và dữ liệu tổng hợp được cung cấp trong ngữ cảnh.

        Phạm vi được phép:
        - Tài khoản, đăng nhập, duyệt người dùng, tổ chức, khoa/phòng, vai trò, nhóm và phân quyền.
        - Quy trình kỹ thuật, tạo mới, phê duyệt, phiên bản, lưu trữ, PDF, lưu đồ, bước thực hiện và kiểm soát ban hành.
        - Danh mục kỹ thuật, tài nguyên, định mức, vật tư, hóa chất, thiết bị, chỉ định và điều phối.
        - Phác đồ, lâm sàng, hồ sơ áp dụng phác đồ, ký xác nhận nội bộ, báo cáo, nhật ký, thông báo và cài đặt trợ lý AI.

        Quy tắc bắt buộc:
        - Không trả lời câu hỏi ngoài QLCM Pro. Nếu người dùng hỏi ngoài phạm vi, nói: "Mình chỉ hỗ trợ các nghiệp vụ trong QLCM Pro" rồi gợi ý họ hỏi về quy trình, lâm sàng, phác đồ, phân quyền hoặc báo cáo.
        - Không hiển thị route kỹ thuật, URL, path, tên file, class, API nội bộ, SQL hoặc đường dẫn quản trị kỹ thuật cho người dùng cuối. Khi hướng dẫn thao tác, dùng tên menu/breadcrumb tiếng Việt, ví dụ "Tổ chức > Người dùng" hoặc "Quy trình > Phê duyệt quy trình".
        - Không chẩn đoán, kê đơn, tư vấn điều trị hoặc đưa ra quyết định lâm sàng.
        - Không yêu cầu, suy đoán hoặc lặp lại thông tin định danh bệnh nhân.
        - Không khẳng định có tích hợp HIS, EMR, kho, dược hoặc thiết bị bên ngoài; hiện chỉ có service boundary nội bộ.
        - Chữ ký hồ sơ chỉ là xác nhận nội bộ bằng tài khoản đăng nhập, thời điểm, ảnh chữ ký vẽ tay nếu có và nhật ký kiểm soát; không gọi nhà cung cấp ký số bên ngoài.
        - Hành động từ chat chỉ được điều hướng hoặc tạo bản nháp một lần trong sessionStorage; không tự ghi SQL.
        - Phân biệt API draft trong tài liệu kiến trúc với runtime Blazor đang triển khai.
        - Nếu thiếu dữ liệu hoặc người dùng không có quyền mở mục phù hợp, nói rõ giới hạn và hướng dẫn kiểm tra trong ứng dụng bằng tên mục nghiệp vụ.
        """;

    public const string OutOfScopeReply =
        "Mình chỉ hỗ trợ các nghiệp vụ trong QLCM Pro. Bạn có thể hỏi về quy trình kỹ thuật, lâm sàng, phác đồ, phân quyền, tài nguyên/định mức, báo cáo, nhật ký hoặc cài đặt trợ lý AI.";

    private static readonly string[] ProjectScopeTerms =
    [
        "qlcm", "he thong", "phan mem", "ung dung", "huong dan", "su dung", "chuc nang",
        "dang nhap", "mat khau", "doi mat khau", "tai khoan", "nguoi dung", "to chuc",
        "khoa phong", "vai tro", "nhom", "phan quyen", "duyet", "tu choi", "gui lai",
        "quy trinh", "sop", "buoc", "ban hanh", "phien ban", "luu tru", "pdf", "luu do",
        "danh muc", "ky thuat", "tai nguyen", "dinh muc", "vat tu", "hoa chat", "thiet bi",
        "chi dinh", "dieu phoi", "phac do", "lam sang", "ho so", "ap dung", "chu ky",
        "ky xac nhan", "thu hoi ky", "bao cao", "nhat ky", "audit", "thong bao", "cai dat",
        "chatbot", "tro ly", "ai", "gemini", "xin chao", "chao", "hello", "hi"
    ];

    public static IReadOnlyList<QlcmChatbotKnowledgeTopic> Topics { get; } =
    [
        new(
            "account-onboarding",
            ["dang ky", "dang nhap", "mat khau", "tai khoan", "nguoi dung", "onboarding", "kich hoat", "tu choi"],
            "Tài khoản công khai đăng ký ở trạng thái chờ duyệt. Quản trị viên duyệt, từ chối hoặc gửi lại yêu cầu trước khi người dùng đăng nhập.",
            "Luồng tài khoản: người dùng đăng ký -> quản trị viên mở Tổ chức > Người dùng -> duyệt hoặc từ chối -> tài khoản được kích hoạt mới đăng nhập được."),
        new(
            "permissions",
            ["phan quyen", "vai tro", "nhom", "quyen", "phe duyet quyen", "khoa phong"],
            "Quyền hiệu lực được giải quyết từ vai trò, nhóm và ghi đè người dùng theo khoa/phòng. Priority cao hơn thắng; cùng priority thì deny thắng. Thay đổi quyền có luồng nháp, gửi duyệt, áp dụng ngay hoặc theo lịch.",
            "Phân quyền gồm vai trò, nhóm và ghi đè cá nhân theo khoa/phòng. Thay đổi cần lý do, gửi duyệt, sau đó áp dụng ngay hoặc lên lịch; hệ thống lưu nhật ký và gửi thông báo."),
        new(
            "procedures",
            ["quy trinh", "sop", "buoc", "ban hanh", "version", "phien ban", "phe duyet", "luu tru", "pdf", "luu do"],
            "Quy trình kỹ thuật có bản nháp, bước tuần tự, role phụ trách, SLA, mapping màn hình và chế độ warn/block. Phiên bản đi từ draft -> pending_approval -> active; bản active cũ thành superseded.",
            "Quy trình kỹ thuật: tạo bản nháp -> khai báo bước, vai trò, định mức và màn hình liên kết -> mở Quy trình > Phê duyệt quy trình để ban hành. Runtime guard có thể cảnh báo hoặc chặn thao tác lệch quy trình."),
        new(
            "resources-orders",
            ["tai nguyen", "dinh muc", "vat tu", "thuoc", "hoa chat", "thiet bi", "chi dinh", "dieu phoi", "ton kho"],
            "Danh mục kỹ thuật liên kết định mức vật tư, thuốc, hóa chất và thiết bị. Khi tạo chỉ định, hệ thống tạo snapshot nguồn lực nội bộ; chưa có API kho/dược thật. Chỉ định đi ordered -> scheduled -> in_progress -> completed hoặc cancelled.",
            "Tài nguyên và chỉ định: cấu hình định mức -> tạo chỉ định -> kiểm tra snapshot nguồn lực -> điều phối thực hiện -> ghi tiêu hao thực tế -> xem báo cáo chênh lệch. Snapshot hiện là dữ liệu nội bộ, chưa phải tồn kho kho/dược thật."),
        new(
            "protocols",
            ["phac do", "icd", "lam sang", "chong chi dinh", "ap dung"],
            "Phác đồ có version và rule ICD, tuổi, giới, khoa, chống chỉ định. Hệ thống chỉ gợi ý phác đồ active phù hợp; người dùng chuyên môn quyết định áp dụng.",
            "Phác đồ: tạo bản nháp -> khai báo rule áp dụng/chống chỉ định -> gửi duyệt và ban hành -> tra cứu gợi ý theo ICD -> người dùng chuyên môn chọn áp dụng. Trợ lý không thay thế quyết định lâm sàng."),
        new(
            "signatures",
            ["chu ky", "ky xac nhan", "ky noi bo", "thu hoi ky", "signature"],
            "Áp dụng phác đồ có thể chuyển applied -> signed -> revoked. Chữ ký là xác nhận nội bộ bằng tài khoản đăng nhập, thời điểm, metadata và ảnh chữ ký vẽ tay; không gọi nhà cung cấp bên ngoài. Thu hồi bắt buộc nhập lý do và lưu nhật ký.",
            "Chữ ký hồ sơ: mở Lâm sàng -> chọn hồ sơ đã áp dụng phác đồ -> bấm Ký -> vẽ chữ ký nội bộ -> xác nhận. Hệ thống gắn chữ ký với tài khoản và thời điểm; thu hồi chữ ký phải nhập lý do và lưu nhật ký."),
        new(
            "audit-reports-notifications",
            ["bao cao", "audit", "nhat ky", "thong bao", "signalr", "tieu thu"],
            "Audit log là append-only. Báo cáo có tổng hợp và tiêu thụ so với định mức. Thông báo được lưu SQL và fan-out realtime qua SignalR.",
            "Bạn có thể dùng Báo cáo để xem tổng hợp và tiêu thụ, Nhật ký để truy vết thay đổi, Thông báo để đọc cập nhật realtime đã lưu trong hệ thống."),
        new(
            "settings",
            ["cai dat", "gemini", "chatbot", "tro ly", "api key", "giao dien"],
            "Cài đặt AI chỉ thay đổi model tương thích provider và phần tùy chỉnh giọng trả lời. Core knowledge và quy tắc an toàn luôn bắt buộc. API key phải nạp từ cấu hình bảo mật của hệ thống.",
            "Trong Cài đặt > Trợ lý AI, bạn có thể chọn model tương thích và thêm hướng dẫn trả lời. Không nhập khóa API vào nội dung chat hoặc các trường nghiệp vụ thông thường.")
    ];

    public static bool IsProjectScoped(string? query)
    {
        var normalized = Normalize(query);
        if (normalized.Length == 0) return true;

        return ProjectScopeTerms.Any(term => normalized.Contains(Normalize(term), StringComparison.Ordinal)) ||
            FindRelevant(normalized, limit: 1).Count > 0;
    }

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
