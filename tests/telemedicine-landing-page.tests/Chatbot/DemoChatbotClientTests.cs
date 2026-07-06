using TelemedicineLandingPage.Models.Admin;
using TelemedicineLandingPage.Models.Chatbot;
using TelemedicineLandingPage.Services.Admin;
using TelemedicineLandingPage.Services.Chatbot;

namespace telemedicine_landing_page.tests.Chatbot;

public class DemoChatbotClientTests
{
    [Fact]
    public async Task YieldsChunksWithDemoPrefix()
    {
        var client = new DemoChatbotClient(new UserPreferencesService());
        var messages = new[]
        {
            new ChatMessage(Guid.NewGuid(), ChatRole.User, "Hỏi về quy trình", DateTime.Now),
        };

        var collected = new List<string>();
        await foreach (var chunk in client.StreamReplyAsync(messages, CancellationToken.None))
        {
            collected.Add(chunk);
        }

        Assert.NotEmpty(collected);
        Assert.True(collected.Count >= 2, "Demo client should split the reply into multiple chunks.");
        var combined = string.Concat(collected);
        Assert.StartsWith("[Chế độ demo - chưa cấu hình API key]", combined);
        Assert.Equal("Quy trình", "Quy trình"); // sanity for diacritic encoding
        Assert.Contains("Quy trình", combined);
    }

    [Fact]
    public async Task RespondsToVietnameseGreeting()
    {
        var client = new DemoChatbotClient(new UserPreferencesService());
        var messages = new[]
        {
            new ChatMessage(Guid.NewGuid(), ChatRole.User, "xin chao", DateTime.Now),
        };

        var combined = await ConcatAsync(client.StreamReplyAsync(messages, CancellationToken.None));
        Assert.Contains("Xin chào", combined);
    }

    [Fact]
    public async Task RoutesToProcedureKeyword()
    {
        var client = new DemoChatbotClient(new UserPreferencesService());
        var messages = new[]
        {
            new ChatMessage(Guid.NewGuid(), ChatRole.User, "quy trinh", DateTime.Now),
        };

        var combined = await ConcatAsync(client.StreamReplyAsync(messages, CancellationToken.None));
        Assert.Contains("Quy trình", combined);
        Assert.Contains("Phê duyệt", combined);
    }

    [Fact]
    public async Task CancellationStopsStreaming()
    {
        var client = new DemoChatbotClient(new UserPreferencesService());
        var messages = new[]
        {
            new ChatMessage(Guid.NewGuid(), ChatRole.User, "Hỏi dài về phác đồ điều trị", DateTime.Now),
        };

        using var cts = new CancellationTokenSource();
        var collected = new List<string>();

        async Task Run()
        {
            try
            {
                await foreach (var chunk in client.StreamReplyAsync(messages, cts.Token))
                {
                    collected.Add(chunk);
                    if (collected.Count == 1)
                    {
                        cts.Cancel();
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        await Run();

        Assert.NotEmpty(collected);
        var combined = string.Concat(collected);
        // The reply should have stopped well before the full demo answer is emitted.
        Assert.True(combined.Length < 600, $"Expected truncated stream, got {combined.Length} characters.");
    }

    [Fact]
    public async Task UsesUserPreferencesPromptHint()
    {
        var prefs = new UserPreferencesService();
        prefs.Update(prefs.Current with { AiSystemPrompt = "Luôn nhắc nhân viên rửa tay đúng quy trình." });
        var client = new DemoChatbotClient(prefs);
        var messages = new[]
        {
            new ChatMessage(Guid.NewGuid(), ChatRole.User, "xin chào", DateTime.Now),
        };

        var combined = await ConcatAsync(client.StreamReplyAsync(messages, CancellationToken.None));
        Assert.Contains("câu lệnh hệ thống", combined);
    }

    private static async Task<string> ConcatAsync(IAsyncEnumerable<string> stream)
    {
        var collected = new List<string>();
        await foreach (var chunk in stream)
        {
            collected.Add(chunk);
        }
        return string.Concat(collected);
    }
}
