using TelemedicineLandingPage.Models.Chatbot;
using TelemedicineLandingPage.Services.Chatbot;

namespace TelemedicineLandingPage.Tests.Chatbot;

public sealed class ChatActionParserTests
{
    [Fact]
    public void TryCreate_ExternalRoute_ReturnsFalse()
    {
        var ok = ChatActionParser.TryCreate(
            ChatActionKind.Navigate,
            "Bad",
            "https://example.com",
            null,
            out var action);

        Assert.False(ok);
        Assert.Equal(string.Empty, action.Route);
    }

    [Fact]
    public void TryCreate_NavigateWithDraft_RequiresNonce()
    {
        var ok = ChatActionParser.TryCreate(
            ChatActionKind.NavigateWithDraft,
            "Draft",
            "/admin/lam-sang",
            null,
            out _);

        Assert.False(ok);
    }

    [Fact]
    public void TryCreate_AllowedDraftRoute_ReturnsActionWithNonce()
    {
        var ok = ChatActionParser.TryCreate(
            ChatActionKind.NavigateWithDraft,
            "Draft",
            "/admin/lam-sang?draft_payload={}",
            "abc123",
            out var action);

        Assert.True(ok);
        Assert.Equal(ChatActionKind.NavigateWithDraft, action.Kind);
        Assert.Equal("/admin/lam-sang", action.Route);
        Assert.Equal("abc123", action.DraftNonce);
    }
}
