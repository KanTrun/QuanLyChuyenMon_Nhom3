using TelemedicineLandingPage.Models.Chatbot;
using TelemedicineLandingPage.Services.Chatbot;

namespace telemedicine_landing_page.tests.Chatbot;

public class ChatbotConversationStoreTests
{
    [Fact]
    public void GreetingMessageInitialised()
    {
        var store = new ChatbotConversationStore();

        Assert.Single(store.Messages);
        var greeting = store.Messages[0];
        Assert.Equal(ChatRole.Assistant, greeting.Role);
        Assert.Contains("Xin chào", greeting.Content);
        Assert.False(greeting.IsStreaming);
    }

    [Fact]
    public void AppendUserAndAssistantPlaceholderAddsTwoMessages()
    {
        var store = new ChatbotConversationStore();
        var raised = 0;
        store.StateChanged += () => raised++;

        store.AppendUser("Xin chào trợ lý");
        var placeholderId = store.AppendAssistantPlaceholder();

        Assert.Equal(3, store.Messages.Count);
        Assert.Equal(ChatRole.User, store.Messages[1].Role);
        Assert.Equal(ChatRole.Assistant, store.Messages[2].Role);
        Assert.True(store.Messages[2].IsStreaming);
        Assert.True(store.IsStreaming);
        Assert.Equal(placeholderId, store.Messages[2].Id);
        Assert.True(raised >= 2);
    }

    [Fact]
    public void StreamingAppendsToPlaceholder()
    {
        var store = new ChatbotConversationStore();
        store.AppendUser("Hỏi về quy trình");
        var id = store.AppendAssistantPlaceholder();

        store.AppendAssistantChunk(id, "Đang ");
        store.AppendAssistantChunk(id, "trả ");
        store.AppendAssistantChunk(id, "lời.");
        store.MarkStreamingComplete(id);

        var assistant = store.Messages.Last();
        Assert.Equal(id, assistant.Id);
        Assert.Equal("Đang trả lời.", assistant.Content);
        Assert.False(assistant.IsStreaming);
        Assert.False(store.IsStreaming);
    }

    [Fact]
    public void ClearReturnsToGreetingOnly()
    {
        var store = new ChatbotConversationStore();
        store.AppendUser("một");
        var id = store.AppendAssistantPlaceholder();
        store.AppendAssistantChunk(id, "phản hồi");
        store.MarkStreamingComplete(id);

        store.Clear();

        Assert.Single(store.Messages);
        Assert.Contains("Xin chào", store.Messages[0].Content);
        Assert.False(store.IsStreaming);
    }

    [Fact]
    public void ReplaceAssistantContentSwapsPlaceholderText()
    {
        var store = new ChatbotConversationStore();
        var id = store.AppendAssistantPlaceholder();
        store.ReplaceAssistantContent(id, "Lỗi mạng.");

        Assert.Equal("Lỗi mạng.", store.Messages.Last().Content);
    }
}
