using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TelemedicineLandingPage.Models.Chatbot;
using TelemedicineLandingPage.Services.Admin;
using TelemedicineLandingPage.Services.Chatbot;

namespace telemedicine_landing_page.tests.Chatbot;

public class GeminiChatbotClientTests
{
    [Fact]
    public async Task ServiceProvider_CreatesTypedGeminiClientWithInjectedContextBuilder()
    {
        var services = CreateClientServices("Gemini");
        var handler = new RecordingHandler(CreateGeminiSse("Gemini ready.", "STOP"));
        services.AddHttpClient<GeminiChatbotClient>(http =>
            {
                http.BaseAddress = new Uri("https://generativelanguage.googleapis.com");
            })
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var client = scope.ServiceProvider.GetRequiredService<GeminiChatbotClient>();
        var combined = await CollectAsync(client.StreamReplyAsync(CreateUserConversation(), CancellationToken.None));

        Assert.Equal("Gemini ready.", combined);
        Assert.NotNull(handler.LastBody);
        using var body = JsonDocument.Parse(handler.LastBody!);
        var system = body.RootElement.GetProperty("system_instruction")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();
        Assert.Equal(TestChatbotContextBuilder.SystemPrompt, system);
    }

    [Fact]
    public async Task ServiceProvider_CreatesTypedAnthropicClientWithInjectedContextBuilder()
    {
        var services = CreateClientServices("Anthropic");
        var handler = new RecordingHandler(CreateAnthropicSse("Anthropic ready."));
        services.AddHttpClient<AnthropicChatbotClient>(http =>
            {
                http.BaseAddress = new Uri("https://api.anthropic.com");
            })
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var client = scope.ServiceProvider.GetRequiredService<AnthropicChatbotClient>();
        var combined = await CollectAsync(client.StreamReplyAsync(CreateUserConversation(), CancellationToken.None));

        Assert.Equal("Anthropic ready.", combined);
        Assert.NotNull(handler.LastBody);
        using var body = JsonDocument.Parse(handler.LastBody!);
        Assert.Equal(TestChatbotContextBuilder.SystemPrompt, body.RootElement.GetProperty("system").GetString());
    }

    [Fact]
    public async Task StreamReplyAsync_ShapesRequestAndConcatenatesSseDeltas()
    {
        var sse = new StringBuilder();
        sse.AppendLine("data: " + JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        role = "model",
                        parts = new[] { new { text = "Xin " } },
                    },
                },
            },
        }));
        sse.AppendLine();
        sse.AppendLine("data: " + JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        role = "model",
                        parts = new[] { new { text = "chào " } },
                    },
                },
            },
        }));
        sse.AppendLine();
        sse.AppendLine("data: " + JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        role = "model",
                        parts = new[] { new { text = "bạn." } },
                    },
                    finishReason = "STOP",
                },
            },
        }));
        sse.AppendLine();

        var handler = new RecordingHandler(sse.ToString());
        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com"),
        };

        var options = Options.Create(new ChatbotOptions
        {
            Provider = "Gemini",
            Model = "gemini-2.5-flash",
            BaseUrl = "https://generativelanguage.googleapis.com",
            ApiKey = "test-api-key",
            MaxTokens = 256,
            SystemPrompt = "Trợ lý nội bộ.",
        });
        var monitor = new StaticOptionsMonitor<ChatbotOptions>(options.Value);

        var prefs = new UserPreferencesService();
        prefs.Update(prefs.Current with
        {
            AiModel = "gemini-2.5-flash",
            AiSystemPrompt = "Trợ lý nội bộ.",
            AiTemperature = 0.4,
        });
        var client = new GeminiChatbotClient(http, monitor, prefs);

        var messages = new[]
        {
            new ChatMessage(Guid.NewGuid(), ChatRole.Assistant, "Xin chào!", DateTime.Now),
            new ChatMessage(Guid.NewGuid(), ChatRole.User, "Cho tôi quy trình", DateTime.Now),
        };

        var collected = new List<string>();
        await foreach (var chunk in client.StreamReplyAsync(messages, CancellationToken.None))
        {
            collected.Add(chunk);
        }

        var combined = string.Concat(collected);
        Assert.Equal("Xin chào bạn.", combined);

        Assert.NotNull(handler.LastRequest);
        var uri = handler.LastRequest!.RequestUri!.ToString();
        Assert.True(
            uri.Contains("/v1beta/models/gemini-2.5-flash:streamGenerateContent", StringComparison.Ordinal),
            $"Expected streamGenerateContent path, got URI: {uri}");
        Assert.Contains("alt=sse", uri, StringComparison.Ordinal);
        Assert.DoesNotContain("key=", uri, StringComparison.Ordinal);
        Assert.True(handler.LastRequest.Headers.TryGetValues("x-goog-api-key", out var keyValues));
        Assert.Equal("test-api-key", keyValues.Single());

        Assert.NotNull(handler.LastBody);
        using var body = JsonDocument.Parse(handler.LastBody!);
        var root = body.RootElement;

        var systemText = root.GetProperty("system_instruction")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();
        Assert.Contains("trợ lý vận hành nội bộ", systemText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Trợ lý nội bộ.", systemText, StringComparison.Ordinal);

        var contents = root.GetProperty("contents");
        Assert.Equal(1, contents.GetArrayLength());
        Assert.Equal("user", contents[0].GetProperty("role").GetString());
        Assert.Equal("Cho tôi quy trình",
            contents[0].GetProperty("parts")[0].GetProperty("text").GetString());

        var generationConfig = root.GetProperty("generationConfig");
        Assert.Equal(256, generationConfig.GetProperty("maxOutputTokens").GetInt32());
        Assert.True(generationConfig.GetProperty("temperature").GetDouble() is >= 0 and <= 1);
    }

    [Fact]
    public async Task StreamReplyAsync_StripsUiGreetingBeforeFirstUserTurn()
    {
        var handler = new RecordingHandler(CreateGeminiSse("OK", "STOP"));
        var client = CreateDirectGeminiClient(handler);

        var messages = new[]
        {
            new ChatMessage(Guid.NewGuid(), ChatRole.Assistant, ChatbotConversationStore.GreetingText, DateTime.Now),
            new ChatMessage(Guid.NewGuid(), ChatRole.User, "hi", DateTime.Now),
        };

        _ = await CollectAsync(client.StreamReplyAsync(messages, CancellationToken.None));

        Assert.NotNull(handler.LastBody);
        using var body = JsonDocument.Parse(handler.LastBody!);
        var contents = body.RootElement.GetProperty("contents");
        Assert.Equal(1, contents.GetArrayLength());
        Assert.Equal("user", contents[0].GetProperty("role").GetString());
        Assert.Equal("hi", contents[0].GetProperty("parts")[0].GetProperty("text").GetString());
    }

    [Fact]
    public void BuildGeminiContents_MergesConsecutiveUserTurns()
    {
        var messages = new[]
        {
            new ChatMessage(Guid.NewGuid(), ChatRole.User, "line 1", DateTime.Now),
            new ChatMessage(Guid.NewGuid(), ChatRole.User, "line 2", DateTime.Now),
        };

        var contents = GeminiChatbotClient.BuildGeminiContents(messages);

        Assert.Single(contents);
        using var body = JsonDocument.SerializeToElement(contents[0]);
        Assert.Equal("user", body.GetProperty("role").GetString());
        Assert.Equal("line 1\n\nline 2", body.GetProperty("parts")[0].GetProperty("text").GetString());
    }

    [Fact]
    public async Task StreamReplyAsync_HttpErrorYieldsVietnameseFailureMessage()
    {
        var handler = new StatusHandler(HttpStatusCode.Unauthorized);
        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com"),
        };

        var monitor = new StaticOptionsMonitor<ChatbotOptions>(new ChatbotOptions
        {
            Provider = "Gemini",
            Model = "gemini-2.5-flash",
            BaseUrl = "https://generativelanguage.googleapis.com",
            ApiKey = "bad-key",
        });

        var client = new GeminiChatbotClient(http, monitor, new UserPreferencesService());

        var combined = string.Empty;
        await foreach (var chunk in client.StreamReplyAsync(
            new[] { new ChatMessage(Guid.NewGuid(), ChatRole.User, "test", DateTime.Now) },
            CancellationToken.None))
        {
            combined += chunk;
        }

        Assert.Contains("HTTP 401", combined, StringComparison.Ordinal);
        Assert.Contains("Trợ lý", combined, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("MAX_TOKENS")]
    [InlineData("SAFETY")]
    public async Task StreamReplyAsync_FinishReasonYieldsActionableNotice(string finishReason)
    {
        var sse = CreateGeminiSse(string.Empty, finishReason);
        var client = CreateDirectGeminiClient(new RecordingHandler(sse));

        var combined = await CollectAsync(client.StreamReplyAsync(CreateUserConversation(), CancellationToken.None));

        Assert.Contains(finishReason, combined, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StreamReplyAsync_MetadataOnlyChunkDoesNotStopFollowingContent()
    {
        var sse = CreateGeminiSse(new { usageMetadata = new { totalTokenCount = 12 } }) +
            CreateGeminiSse("Xin chào.", "STOP");
        var client = CreateDirectGeminiClient(new RecordingHandler(sse));

        var combined = await CollectAsync(client.StreamReplyAsync(CreateUserConversation(), CancellationToken.None));

        Assert.Equal("Xin chào.", combined);
    }

    [Fact]
    public async Task StreamReplyAsync_PromptSafetyBlockYieldsNoCandidateNotice()
    {
        var sse = CreateGeminiSse(new
        {
            candidates = Array.Empty<object>(),
            promptFeedback = new { blockReason = "SAFETY" },
        });
        var client = CreateDirectGeminiClient(new RecordingHandler(sse));

        var combined = await CollectAsync(client.StreamReplyAsync(CreateUserConversation(), CancellationToken.None));

        Assert.Contains("không trả về phương án", combined, StringComparison.Ordinal);
        Assert.Contains("SAFETY", combined, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StreamReplyAsync_EmptyCandidatesYieldNoCandidateNotice()
    {
        var sse = CreateGeminiSse(new { candidates = Array.Empty<object>() });
        var client = CreateDirectGeminiClient(new RecordingHandler(sse));

        var combined = await CollectAsync(client.StreamReplyAsync(CreateUserConversation(), CancellationToken.None));

        Assert.Contains("không trả về phương án", combined, StringComparison.Ordinal);
    }

    private static GeminiChatbotClient CreateDirectGeminiClient(HttpMessageHandler handler)
    {
        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com"),
        };
        var monitor = new StaticOptionsMonitor<ChatbotOptions>(new ChatbotOptions
        {
            ApiKey = "test-api-key",
        });
        return new GeminiChatbotClient(http, monitor, new UserPreferencesService());
    }

    private static ChatMessage[] CreateUserConversation()
        => [new ChatMessage(Guid.NewGuid(), ChatRole.User, "test", DateTime.Now)];

    private static async Task<string> CollectAsync(IAsyncEnumerable<string> stream)
    {
        var combined = new StringBuilder();
        await foreach (var chunk in stream)
        {
            combined.Append(chunk);
        }
        return combined.ToString();
    }

    private static string CreateGeminiSse(string text, string finishReason)
        => CreateGeminiSse(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        role = "model",
                        parts = new[] { new { text } },
                    },
                    finishReason,
                },
            },
        });

    private static string CreateGeminiSse(object payload)
        => $"data: {JsonSerializer.Serialize(payload)}{Environment.NewLine}{Environment.NewLine}";

    private static string CreateAnthropicSse(string text)
    {
        var delta = JsonSerializer.Serialize(new
        {
            type = "content_block_delta",
            delta = new { type = "text_delta", text },
        });
        var stop = JsonSerializer.Serialize(new { type = "message_stop" });
        return $"data: {delta}{Environment.NewLine}{Environment.NewLine}" +
            $"data: {stop}{Environment.NewLine}{Environment.NewLine}";
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly string _sseBody;

        public RecordingHandler(string sseBody) => _sseBody = sseBody;

        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content is not null)
            {
                LastBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_sseBody, Encoding.UTF8, "text/event-stream"),
            };
            return response;
        }
    }

    private sealed class StatusHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;

        public StatusHandler(HttpStatusCode status) => _status = status;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(string.Empty),
            });
        }
    }

    private sealed class StaticOptionsMonitor<T> : IOptionsMonitor<T> where T : class
    {
        private readonly T _value;

        public StaticOptionsMonitor(T value) => _value = value;

        public T CurrentValue => _value;

        public T Get(string? name) => _value;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    private static ServiceCollection CreateClientServices(string provider)
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.Configure<ChatbotOptions>(options =>
        {
            options.Provider = provider;
            options.Model = provider == "Anthropic" ? "claude-sonnet-4-5-20250929" : "gemini-2.5-flash";
            options.BaseUrl = provider == "Anthropic"
                ? "https://api.anthropic.com"
                : "https://generativelanguage.googleapis.com";
            options.ApiKey = "test-api-key";
        });
        services.AddScoped<IUserPreferencesService, UserPreferencesService>();
        services.AddScoped<IChatbotContextBuilder, TestChatbotContextBuilder>();
        return services;
    }

    private sealed class TestChatbotContextBuilder : IChatbotContextBuilder
    {
        public const string SystemPrompt = "Injected rich chatbot context.";

        public string BuildSystemPrompt(
            IReadOnlyList<ChatMessage> conversation,
            string? configuredPrompt,
            string? userPrompt)
            => SystemPrompt;
    }
}
