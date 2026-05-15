using TelemedicineLandingPage.Models.Chatbot;

namespace TelemedicineLandingPage.Services.Chatbot;

/// <summary>
/// High-level orchestrator the UI binds to. Wraps an <see cref="IChatbotClient"/>
/// and an <see cref="IChatbotConversationStore"/> so that the panel only needs
/// to call <see cref="SendAsync"/>, <see cref="ClearAsync"/> and
/// <see cref="CancelAsync"/>.
/// </summary>
public interface IChatbotService
{
    IReadOnlyList<ChatMessage> Messages { get; }

    bool IsStreaming { get; }

    string ProviderLabel { get; }

    event Action? StateChanged;

    Task SendAsync(string userInput, CancellationToken ct = default);

    Task ClearAsync();

    Task CancelAsync();
}
