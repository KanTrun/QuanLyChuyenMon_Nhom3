using TelemedicineLandingPage.Models.Chatbot;

namespace TelemedicineLandingPage.Services.Chatbot;

/// <summary>
/// Default <see cref="IChatbotService"/>. Coordinates the conversation store
/// with the streaming client, ensures the UI sees a Vietnamese error message
/// on failure, and exposes a cancellation hook for the "Dừng" button.
/// </summary>
public sealed class ChatbotService : IChatbotService, IDisposable
{
    private const string FailureFallback =
        "Xin lỗi, trợ lý không thể phản hồi lúc này. Vui lòng thử lại.";

    private readonly IChatbotClient _client;
    private readonly IChatbotConversationStore _store;
    private readonly IChatbotPrivacyGuard _privacyGuard;
    private readonly object _gate = new();
    private CancellationTokenSource? _streamCts;

    public ChatbotService(IChatbotClient client, IChatbotConversationStore store)
        : this(client, store, new ChatbotPrivacyGuard())
    {
    }

    public ChatbotService(
        IChatbotClient client,
        IChatbotConversationStore store,
        IChatbotPrivacyGuard privacyGuard)
    {
        _client = client;
        _store = store;
        _privacyGuard = privacyGuard;
        _store.StateChanged += OnStoreChanged;
    }

    public IReadOnlyList<ChatMessage> Messages => _store.Messages;

    public bool IsStreaming => _store.IsStreaming;

    public string ProviderLabel => _client.ProviderLabel;

    public event Action? StateChanged;

    public async Task SendAsync(string userInput, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userInput)) return;

        var trimmed = userInput.Trim();
        if (!_privacyGuard.CanSend(trimmed, out var localReply))
        {
            _store.AppendUser(ChatbotPrivacyGuard.BlockedUserMarker);
            var blockedAssistantId = _store.AppendAssistantPlaceholder();
            _store.ReplaceAssistantContent(blockedAssistantId, localReply ?? FailureFallback);
            _store.MarkStreamingComplete(blockedAssistantId);
            return;
        }

        _store.AppendUser(trimmed);
        var assistantId = _store.AppendAssistantPlaceholder();

        CancellationTokenSource linkedCts;
        lock (_gate)
        {
            _streamCts?.Cancel();
            _streamCts?.Dispose();
            _streamCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            linkedCts = _streamCts;
        }

        var hasContent = false;
        try
        {
            var snapshot = _store.Messages
                .Where(m => m.Id != assistantId)
                .ToArray();

            await foreach (var chunk in _client
                .StreamReplyAsync(snapshot, linkedCts.Token)
                .WithCancellation(linkedCts.Token)
                .ConfigureAwait(false))
            {
                if (linkedCts.IsCancellationRequested) break;
                if (string.IsNullOrEmpty(chunk)) continue;
                _store.AppendAssistantChunk(assistantId, chunk);
                hasContent = true;
            }
        }
        catch (OperationCanceledException)
        {
            // Cancellation is a normal flow when the user clicks "Dừng".
        }
        catch (Exception)
        {
            _store.ReplaceAssistantContent(assistantId, FailureFallback);
            hasContent = true;
        }
        finally
        {
            if (!hasContent)
            {
                // The stream ended without producing any text (e.g. the user cancelled
                // immediately). Leave a short marker so the placeholder isn't left blank.
                var current = _store.Messages.FirstOrDefault(m => m.Id == assistantId);
                if (current is not null && string.IsNullOrEmpty(current.Content))
                {
                    _store.ReplaceAssistantContent(assistantId, "(Đã dừng phản hồi.)");
                }
            }
            _store.MarkStreamingComplete(assistantId);

            lock (_gate)
            {
                if (_streamCts == linkedCts)
                {
                    _streamCts.Dispose();
                    _streamCts = null;
                }
            }
        }
    }

    public Task ClearAsync()
    {
        CancellationTokenSource? toCancel;
        lock (_gate)
        {
            toCancel = _streamCts;
            _streamCts = null;
        }
        try
        {
            toCancel?.Cancel();
        }
        catch (ObjectDisposedException) { }
        toCancel?.Dispose();
        _store.Clear();
        return Task.CompletedTask;
    }

    public Task CancelAsync()
    {
        CancellationTokenSource? toCancel;
        lock (_gate)
        {
            toCancel = _streamCts;
        }
        try
        {
            toCancel?.Cancel();
        }
        catch (ObjectDisposedException) { }
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _store.StateChanged -= OnStoreChanged;
        CancellationTokenSource? cts;
        lock (_gate)
        {
            cts = _streamCts;
            _streamCts = null;
        }
        try
        {
            cts?.Cancel();
        }
        catch (ObjectDisposedException) { }
        cts?.Dispose();
    }

    private void OnStoreChanged() => StateChanged?.Invoke();
}
