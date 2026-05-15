using System.Runtime.CompilerServices;
using TelemedicineLandingPage.Models.Chatbot;
using TelemedicineLandingPage.Services.Chatbot;

namespace telemedicine_landing_page.tests.Chatbot;

public class ChatbotServiceTests
{
    [Fact]
    public async Task SendAsync_StreamsChunksIntoAssistantMessage()
    {
        var store = new ChatbotConversationStore();
        var client = new RecordingChatbotClient(new[] { "Xin ", "chào ", "bạn." });
        var service = new ChatbotService(client, store);

        await service.SendAsync("Xin chào", CancellationToken.None);

        Assert.False(service.IsStreaming);
        var last = service.Messages.Last();
        Assert.Equal(ChatRole.Assistant, last.Role);
        Assert.Equal("Xin chào bạn.", last.Content);
        Assert.False(last.IsStreaming);
    }

    [Fact]
    public async Task SendAsync_PassesConversationWithoutCurrentPlaceholder()
    {
        var store = new ChatbotConversationStore();
        var client = new RecordingChatbotClient(new[] { "ok" });
        var service = new ChatbotService(client, store);

        await service.SendAsync("test", CancellationToken.None);

        Assert.NotNull(client.LastConversation);
        // Greeting + user message; the placeholder must NOT be sent to the client.
        Assert.Equal(2, client.LastConversation!.Count);
        Assert.Equal(ChatRole.Assistant, client.LastConversation[0].Role);
        Assert.Equal(ChatRole.User, client.LastConversation[1].Role);
        Assert.Equal("test", client.LastConversation[1].Content);
    }

    [Fact]
    public async Task SendAsync_OnException_ReplacesWithVietnameseFailureMessage()
    {
        var store = new ChatbotConversationStore();
        var client = new ThrowingChatbotClient();
        var service = new ChatbotService(client, store);

        await service.SendAsync("Hỏi gì đó", CancellationToken.None);

        var last = service.Messages.Last();
        Assert.Equal(ChatRole.Assistant, last.Role);
        Assert.Contains("Xin lỗi", last.Content);
        Assert.False(last.IsStreaming);
        Assert.False(service.IsStreaming);
    }

    [Fact]
    public async Task ClearAsync_ResetsToGreetingAndCancelsActiveStream()
    {
        var store = new ChatbotConversationStore();
        var client = new RecordingChatbotClient(new[] { "xin", " chào" });
        var service = new ChatbotService(client, store);

        await service.SendAsync("Hỏi", CancellationToken.None);
        await service.ClearAsync();

        Assert.Single(service.Messages);
        Assert.Contains("Xin chào", service.Messages[0].Content);
    }

    [Fact]
    public void ProviderLabelDelegatesToClient()
    {
        var store = new ChatbotConversationStore();
        var client = new RecordingChatbotClient(Array.Empty<string>()) { Label = "Demo nội bộ" };
        var service = new ChatbotService(client, store);

        Assert.Equal("Demo nội bộ", service.ProviderLabel);
    }

    private sealed class RecordingChatbotClient : IChatbotClient
    {
        private readonly string[] _chunks;

        public RecordingChatbotClient(string[] chunks) => _chunks = chunks;

        public string Label { get; set; } = "Test client";
        public string ProviderLabel => Label;
        public IReadOnlyList<ChatMessage>? LastConversation { get; private set; }

        public async IAsyncEnumerable<string> StreamReplyAsync(
            IReadOnlyList<ChatMessage> conversation,
            [EnumeratorCancellation] CancellationToken ct)
        {
            LastConversation = conversation;
            foreach (var chunk in _chunks)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Yield();
                yield return chunk;
            }
        }
    }

    private sealed class ThrowingChatbotClient : IChatbotClient
    {
        public string ProviderLabel => "Throws";

        public async IAsyncEnumerable<string> StreamReplyAsync(
            IReadOnlyList<ChatMessage> conversation,
            [EnumeratorCancellation] CancellationToken ct)
        {
            await Task.Yield();
            throw new InvalidOperationException("Boom");
#pragma warning disable CS0162 // Unreachable code detected
            yield break;
#pragma warning restore CS0162
        }
    }
}
