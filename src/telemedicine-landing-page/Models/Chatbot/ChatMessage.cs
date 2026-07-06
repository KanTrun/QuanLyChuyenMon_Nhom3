namespace TelemedicineLandingPage.Models.Chatbot;

/// <summary>Role of a single chat turn.</summary>
public enum ChatRole
{
    User,
    Assistant,
    System,
}

/// <summary>
/// A single chat transcript entry. <see cref="IsStreaming"/> is true while the
/// assistant placeholder is still being filled by the streaming client.
/// </summary>
public sealed record ChatMessage(
    Guid Id,
    ChatRole Role,
    string Content,
    DateTime Timestamp,
    bool IsStreaming = false);
