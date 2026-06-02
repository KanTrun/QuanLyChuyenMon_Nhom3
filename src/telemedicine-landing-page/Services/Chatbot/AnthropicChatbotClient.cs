using System.Globalization;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TelemedicineLandingPage.Models.Chatbot;
using TelemedicineLandingPage.Services.Admin;

namespace TelemedicineLandingPage.Services.Chatbot;

/// <summary>
/// Streaming Anthropic Claude client. Uses the Messages SSE endpoint and yields
/// each <c>text_delta</c> as it arrives. Errors are converted to a Vietnamese
/// inline message so the UI never sees a thrown exception.
/// </summary>
public sealed class AnthropicChatbotClient : IChatbotClient
{
    private static readonly JsonSerializerOptions BodyOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IOptionsMonitor<ChatbotOptions> _options;
    private readonly IUserPreferencesService _preferences;
    private readonly IChatbotContextBuilder _contextBuilder;

    [ActivatorUtilitiesConstructor]
    public AnthropicChatbotClient(
        HttpClient http,
        IOptionsMonitor<ChatbotOptions> options,
        IUserPreferencesService preferences)
        : this(http, options, preferences, new CoreOnlyChatbotContextBuilder())
    {
    }

    public AnthropicChatbotClient(
        HttpClient http,
        IOptionsMonitor<ChatbotOptions> options,
        IUserPreferencesService preferences,
        IChatbotContextBuilder contextBuilder)
    {
        _http = http;
        _options = options;
        _preferences = preferences;
        _contextBuilder = contextBuilder;
    }

    public string ProviderLabel => "Anthropic Claude";

    public async IAsyncEnumerable<string> StreamReplyAsync(
        IReadOnlyList<ChatMessage> conversation,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var opts = _options.CurrentValue;
        var prefs = _preferences.Current;

        var model = ChatbotModelCatalog.Resolve(opts.Provider, opts.Model, prefs.AiModel);
        var system = _contextBuilder.BuildSystemPrompt(conversation, opts.SystemPrompt, prefs.AiSystemPrompt);
        var temperature = Math.Round(Math.Clamp(prefs.AiTemperature, 0d, 1d), 2);

        var messages = conversation
            .Where(m => m.Role != ChatRole.System)
            .Select(m => new
            {
                role = m.Role == ChatRole.User ? "user" : "assistant",
                content = m.Content ?? string.Empty,
            })
            .ToList();

        var payload = new
        {
            model,
            max_tokens = opts.MaxTokens,
            system,
            stream = true,
            temperature,
            messages,
        };

        var (response, failureMessage) = await SendRequestAsync(payload, opts, ct).ConfigureAwait(false);

        if (failureMessage is not null)
        {
            yield return failureMessage;
            yield break;
        }

        if (response is null)
        {
            yield return "[Trợ lý không thể phản hồi: không nhận được dữ liệu]";
            yield break;
        }

        await foreach (var chunk in ReadSseAsync(response, ct).ConfigureAwait(false))
        {
            yield return chunk;
        }
    }

    private async Task<(HttpResponseMessage? Response, string? FailureMessage)> SendRequestAsync(
        object payload,
        ChatbotOptions opts,
        CancellationToken ct)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/v1/messages");
            request.Headers.TryAddWithoutValidation("x-api-key", opts.ApiKey);
            request.Headers.TryAddWithoutValidation("anthropic-version", opts.AnthropicVersion);
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

            var json = JsonSerializer.Serialize(payload, BodyOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var status = (int)response.StatusCode;
                response.Dispose();
                return (null, string.Format(
                    CultureInfo.InvariantCulture,
                    "[Trợ lý đang gặp sự cố: HTTP {0}]",
                    status));
            }

            return (response, null);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return (null, $"[Trợ lý không thể phản hồi: {ex.Message}]");
        }
    }

    private static async IAsyncEnumerable<string> ReadSseAsync(
        HttpResponseMessage response,
        [EnumeratorCancellation] CancellationToken ct)
    {
        Stream? stream = null;
        StreamReader? reader = null;

        try
        {
            stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            reader = new StreamReader(stream, Encoding.UTF8);

            while (!ct.IsCancellationRequested)
            {
                var (line, error) = await ReadLineSafeAsync(reader, ct).ConfigureAwait(false);
                if (error is not null)
                {
                    yield return error;
                    yield break;
                }
                if (line is null)
                {
                    yield break;
                }

                if (line.Length == 0) continue;
                if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;

                var payloadText = line[5..].TrimStart();
                if (payloadText.Length == 0) continue;
                if (payloadText == "[DONE]") yield break;

                if (!TryParseSseEvent(payloadText, out var delta, out var stop))
                {
                    continue;
                }
                if (stop) yield break;
                if (!string.IsNullOrEmpty(delta)) yield return delta!;
            }
        }
        finally
        {
            reader?.Dispose();
            stream?.Dispose();
            response.Dispose();
        }
    }

    private static async Task<(string? Line, string? Error)> ReadLineSafeAsync(
        StreamReader reader,
        CancellationToken ct)
    {
        try
        {
            var line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
            return (line, null);
        }
        catch (OperationCanceledException)
        {
            return (null, null);
        }
        catch (Exception ex)
        {
            return (null, $"[Trợ lý không thể phản hồi: {ex.Message}]");
        }
    }

    private static bool TryParseSseEvent(string payloadText, out string? delta, out bool stop)
    {
        delta = null;
        stop = false;
        try
        {
            using var doc = JsonDocument.Parse(payloadText);
            if (!doc.RootElement.TryGetProperty("type", out var typeProp))
            {
                return false;
            }
            var type = typeProp.GetString();
            if (type == "message_stop")
            {
                stop = true;
                return true;
            }
            if (type == "content_block_delta"
                && doc.RootElement.TryGetProperty("delta", out var deltaProp)
                && deltaProp.TryGetProperty("type", out var deltaTypeProp)
                && deltaTypeProp.GetString() == "text_delta"
                && deltaProp.TryGetProperty("text", out var textProp))
            {
                delta = textProp.GetString();
                return true;
            }
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
