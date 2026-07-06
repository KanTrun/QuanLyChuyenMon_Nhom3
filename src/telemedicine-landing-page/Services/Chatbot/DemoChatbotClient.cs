using System.Runtime.CompilerServices;
using TelemedicineLandingPage.Models.Chatbot;
using TelemedicineLandingPage.Services.Admin;

namespace TelemedicineLandingPage.Services.Chatbot;

/// <summary>
/// Offline-friendly chatbot client used when no Anthropic API key is configured.
/// It produces a deterministic Vietnamese response routed by simple keyword
/// matching and emits the reply in small chunks so the UI exercises streaming.
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
        var normalized = QlcmChatbotKnowledgeCatalog.Normalize(userInput);

        if (string.IsNullOrEmpty(normalized))
        {
            return "Bạn vui lòng nhập câu hỏi cụ thể nhé. Tôi có thể hỗ trợ về tài khoản, quy trình kỹ thuật, phân quyền, tài nguyên, chỉ định, phác đồ, báo cáo, chữ ký nội bộ và cài đặt QLCM Pro.";
        }

        if (Contains(normalized, "chao") || Contains(normalized, "xin chao") || Contains(normalized, "hello"))
        {
            return "Xin chào! Bạn có thể hỏi tôi về tài khoản, quy trình kỹ thuật, phân quyền, tài nguyên, chỉ định, phác đồ, báo cáo hoặc chữ ký nội bộ trong QLCM Pro.";
        }

        var topic = QlcmChatbotKnowledgeCatalog.FindRelevant(normalized, limit: 1).FirstOrDefault();
        if (topic is not null)
        {
            return topic.DemoReply;
        }

        return QlcmChatbotKnowledgeCatalog.OutOfScopeReply;
    }

    private static bool Contains(string source, string needle) =>
        source.Contains(needle, StringComparison.Ordinal);
}
