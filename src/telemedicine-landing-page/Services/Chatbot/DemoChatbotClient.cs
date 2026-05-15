using System.Globalization;
using System.Runtime.CompilerServices;
using TelemedicineLandingPage.Models.Chatbot;
using TelemedicineLandingPage.Services.Admin;

namespace TelemedicineLandingPage.Services.Chatbot;

/// <summary>
/// Offline-friendly chatbot client used when no Anthropic API key is configured.
/// It produces a deterministic Vietnamese response routed by simple keyword
/// matching and emits the reply in ~30-character chunks with a small delay so
/// that the UI exercises the same streaming code path as the live client.
/// </summary>
public sealed class DemoChatbotClient : IChatbotClient
{
    private const string DemoPrefix = "[Chế độ demo - chưa cấu hình API key] ";
    private const int ChunkSize = 30;
    private const int DelayMs = 60;

    private readonly IUserPreferencesService _preferences;

    public DemoChatbotClient(IUserPreferencesService preferences)
    {
        _preferences = preferences;
    }

    public string ProviderLabel => "Demo nội bộ";

    public async IAsyncEnumerable<string> StreamReplyAsync(
        IReadOnlyList<ChatMessage> conversation,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var lastUser = string.Empty;
        for (var i = conversation.Count - 1; i >= 0; i--)
        {
            if (conversation[i].Role == ChatRole.User)
            {
                lastUser = conversation[i].Content ?? string.Empty;
                break;
            }
        }

        var reply = ComposeReply(lastUser);
        var promptHint = BuildPromptHint();
        var full = DemoPrefix + (string.IsNullOrEmpty(promptHint) ? string.Empty : promptHint + "\n\n") + reply;

        var emittedAny = false;
        for (var offset = 0; offset < full.Length; offset += ChunkSize)
        {
            ct.ThrowIfCancellationRequested();
            var length = Math.Min(ChunkSize, full.Length - offset);
            var chunk = full.Substring(offset, length);
            yield return chunk;
            emittedAny = true;
            try
            {
                await Task.Delay(DelayMs, ct).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                yield break;
            }
        }

        if (!emittedAny)
        {
            yield return DemoPrefix;
        }
    }

    private string BuildPromptHint()
    {
        var prompt = _preferences.Current.AiSystemPrompt;
        if (string.IsNullOrWhiteSpace(prompt)) return string.Empty;
        var trimmed = prompt.Trim();
        if (trimmed.Length > 80)
        {
            trimmed = trimmed[..80].TrimEnd() + "...";
        }
        return $"(Áp dụng câu lệnh hệ thống: \"{trimmed}\")";
    }

    private static string ComposeReply(string userInput)
    {
        var normalized = (userInput ?? string.Empty).Trim().ToLower(CultureInfo.InvariantCulture);

        if (string.IsNullOrEmpty(normalized))
        {
            return "Bạn vui lòng nhập câu hỏi cụ thể nhé. Tôi có thể hỗ trợ về quy trình kỹ thuật, phân quyền, danh mục, phác đồ, báo cáo và cài đặt.";
        }

        if (Contains(normalized, "chao") || Contains(normalized, "xin chao") || Contains(normalized, "hello"))
        {
            return "Xin chào! Rất vui được hỗ trợ bạn. Bạn có thể hỏi tôi về **quy trình kỹ thuật**, **phân quyền**, **phác đồ điều trị**, **báo cáo tiêu thụ** hoặc các cài đặt trong hệ thống QLCM Pro.";
        }

        if (Contains(normalized, "quy trinh") || Contains(normalized, "quytrinh"))
        {
            return "Để quản lý **Quy trình kỹ thuật**, bạn có thể:\n- Mở mục `Quy trình` ở thanh điều hướng\n- Bấm `Tạo quy trình mới` để khởi tạo\n- Vào tab `Phê duyệt` để duyệt các quy trình đang chờ";
        }

        if (Contains(normalized, "phan quyen") || Contains(normalized, "phanquyen"))
        {
            return "Trang **Phân quyền** chia làm ba tab: *Vai trò*, *Tài khoản* và *Lịch sử thay đổi*. Mọi thay đổi ma trận quyền đều được ghi nhận kèm lý do để truy vết.";
        }

        if (Contains(normalized, "phac do") || Contains(normalized, "phacdo"))
        {
            return "Mục **Phác đồ** liệt kê các phác đồ điều trị theo chuyên khoa. Bạn có thể:\n- Lọc theo loại phác đồ\n- Mở thẻ phác đồ để xem chống chỉ định\n- Bấm `Áp dụng cho bệnh nhân` để ghi nhận lượt áp dụng";
        }

        if (Contains(normalized, "bao cao") || Contains(normalized, "baocao"))
        {
            return "Trang **Báo cáo** tổng hợp 4 nhóm chỉ tiêu. Báo cáo tiêu thụ vật tư có biểu đồ top sai lệch và hỗ trợ `Xuất CSV` để mở bằng Excel.";
        }

        if (Contains(normalized, "cai dat") || Contains(normalized, "caidat") || Contains(normalized, "settings"))
        {
            return "Vào **Cài đặt** để cập nhật hồ sơ, giao diện, kênh thông báo và *Trợ lý AI*. Sau khi đổi mô hình hoặc câu lệnh hệ thống, hãy bấm `Lưu cấu hình`.";
        }

        return "Tôi đã ghi nhận câu hỏi. Trong chế độ demo, tôi gợi ý bạn xem các mục **Quy trình**, **Phác đồ** hoặc **Báo cáo** trên thanh điều hướng để tra cứu nhanh.";
    }

    private static bool Contains(string source, string needle) =>
        source.Contains(needle, StringComparison.Ordinal);
}
