namespace TelemedicineLandingPage.Models.Chatbot;

public enum ChatActionKind
{
    Navigate,
    NavigateWithDraft,
    OpenDocumentation
}

public sealed record ChatAction(
    ChatActionKind Kind,
    string Label,
    string Route,
    string? DraftNonce);
