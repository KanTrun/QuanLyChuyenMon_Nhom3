using TelemedicineLandingPage.Models.Chatbot;

namespace TelemedicineLandingPage.Services.Chatbot;

/// <summary>
/// Default per-circuit implementation of <see cref="IChatbotConversationStore"/>.
/// Uses a private object lock for any list mutation so that the streaming
/// loop and the UI-thread <c>StateHasChanged</c> never race.
/// </summary>
public sealed class ChatbotConversationStore : IChatbotConversationStore
{
    public const string GreetingText =
        "Xin chào! Tôi là trợ lý AI của QLCM Pro. Tôi có thể giúp bạn tra cứu quy trình, " +
        "phác đồ, phân quyền hay báo cáo. Bạn cần hỗ trợ gì hôm nay?";

    private readonly object _gate = new();
    private readonly List<ChatMessage> _messages = new();
    private bool _streaming;

    public ChatbotConversationStore()
    {
        SeedGreeting();
    }

    public IReadOnlyList<ChatMessage> Messages
    {
        get { lock (_gate) return _messages.ToArray(); }
    }

    public bool IsStreaming
    {
        get { lock (_gate) return _streaming; }
    }

    public event Action? StateChanged;

    public Guid AppendUser(string content)
    {
        var trimmed = (content ?? string.Empty).TrimEnd();
        var id = Guid.NewGuid();
        lock (_gate)
        {
            _messages.Add(new ChatMessage(id, ChatRole.User, trimmed, DateTime.Now, IsStreaming: false));
        }
        Raise();
        return id;
    }

    public Guid AppendAssistantPlaceholder()
    {
        var id = Guid.NewGuid();
        lock (_gate)
        {
            _messages.Add(new ChatMessage(id, ChatRole.Assistant, string.Empty, DateTime.Now, IsStreaming: true));
            _streaming = true;
        }
        Raise();
        return id;
    }

    public void AppendAssistantChunk(Guid id, string chunk)
    {
        if (string.IsNullOrEmpty(chunk)) return;
        lock (_gate)
        {
            var idx = FindIndex(id);
            if (idx < 0) return;
            var existing = _messages[idx];
            _messages[idx] = existing with { Content = existing.Content + chunk };
        }
        Raise();
    }

    public void ReplaceAssistantContent(Guid id, string content)
    {
        lock (_gate)
        {
            var idx = FindIndex(id);
            if (idx < 0) return;
            var existing = _messages[idx];
            _messages[idx] = existing with { Content = content ?? string.Empty };
        }
        Raise();
    }

    public void MarkStreamingComplete(Guid id)
    {
        lock (_gate)
        {
            var idx = FindIndex(id);
            if (idx >= 0)
            {
                var existing = _messages[idx];
                _messages[idx] = existing with { IsStreaming = false };
            }
            _streaming = _messages.Any(m => m.IsStreaming);
        }
        Raise();
    }

    public void Clear()
    {
        lock (_gate)
        {
            _messages.Clear();
            _streaming = false;
            SeedGreeting();
        }
        Raise();
    }

    private int FindIndex(Guid id)
    {
        for (var i = 0; i < _messages.Count; i++)
        {
            if (_messages[i].Id == id) return i;
        }
        return -1;
    }

    private void SeedGreeting()
    {
        _messages.Add(new ChatMessage(
            Guid.NewGuid(),
            ChatRole.Assistant,
            GreetingText,
            DateTime.Now,
            IsStreaming: false));
    }

    private void Raise() => StateChanged?.Invoke();
}
