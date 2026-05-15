using TelemedicineLandingPage.Models.Chatbot;

namespace TelemedicineLandingPage.Services.Chatbot;

/// <summary>
/// Per-circuit conversation buffer. Provides the small mutating surface that
/// <see cref="IChatbotService"/> needs and is testable in isolation.
/// </summary>
public interface IChatbotConversationStore
{
    IReadOnlyList<ChatMessage> Messages { get; }

    /// <summary>True while there is an assistant placeholder still being filled.</summary>
    bool IsStreaming { get; }

    /// <summary>Append a user message and notify subscribers.</summary>
    Guid AppendUser(string content);

    /// <summary>Append an empty assistant placeholder marked as streaming.</summary>
    Guid AppendAssistantPlaceholder();

    /// <summary>Append a chunk to the streaming assistant message.</summary>
    void AppendAssistantChunk(Guid id, string chunk);

    /// <summary>Replace the streaming assistant message body wholesale.</summary>
    void ReplaceAssistantContent(Guid id, string content);

    /// <summary>Mark the assistant message as no longer streaming.</summary>
    void MarkStreamingComplete(Guid id);

    /// <summary>Reset the conversation back to the seeded greeting.</summary>
    void Clear();

    /// <summary>Notification fired after every mutation.</summary>
    event Action? StateChanged;
}
