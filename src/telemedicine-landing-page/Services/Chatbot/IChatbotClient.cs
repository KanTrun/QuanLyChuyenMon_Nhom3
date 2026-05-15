using TelemedicineLandingPage.Models.Chatbot;

namespace TelemedicineLandingPage.Services.Chatbot;

/// <summary>
/// Streaming chat client. Implementations are expected to convert any
/// transport-level failure into a Vietnamese conversation message rather than
/// throwing, so the panel never has to surface raw exceptions.
/// </summary>
public interface IChatbotClient
{
    /// <summary>Friendly provider label rendered in the panel header.</summary>
    string ProviderLabel { get; }

    /// <summary>
    /// Stream the next assistant reply for the supplied conversation. The implementation
    /// must honour <paramref name="ct"/> and stop yielding once cancelled.
    /// </summary>
    IAsyncEnumerable<string> StreamReplyAsync(
        IReadOnlyList<ChatMessage> conversation,
        CancellationToken ct);
}
