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
/// Streaming Google Gemini client. Uses the Generative Language v1beta
/// <c>streamGenerateContent</c> SSE endpoint and yields each text part as it
/// arrives. Errors are converted to a Vietnamese inline message so the UI never
/// sees a thrown exception, mirroring <see cref="AnthropicChatbotClient"/>.
/// </summary>
public sealed class GeminiChatbotClient : IChatbotClient
{
    private static readonly JsonSerializerOptions BodyOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IOptionsMonitor<ChatbotOptions> _options;
    private readonly IUserPreferencesService _preferences;
    private readonly IChatbotContextBuilder _contextBuilder;

    public GeminiChatbotClient(
        HttpClient http,
        IOptionsMonitor<ChatbotOptions> options,
        IUserPreferencesService preferences)
        : this(http, options, preferences, new CoreOnlyChatbotContextBuilder())
    {
    }

    [ActivatorUtilitiesConstructor]
    public GeminiChatbotClient(
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

    public string ProviderLabel => "Google Gemini";

    public async IAsyncEnumerable<string> StreamReplyAsync(
        IReadOnlyList<ChatMessage> conversation,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var opts = _options.CurrentValue;
        var prefs = _preferences.Current;

        var model = ChatbotModelCatalog.Resolve(opts.Provider, opts.Model, prefs.AiModel);
        var system = _contextBuilder.BuildSystemPrompt(conversation, opts.SystemPrompt, prefs.AiSystemPrompt);
        var temperature = Math.Round(Math.Clamp(prefs.AiTemperature, 0d, 1d), 2);

        var contents = conversation
            .Where(m => m.Role != ChatRole.System)
            .Select(m => new
            {
                role = m.Role == ChatRole.User ? "user" : "model",
                parts = new[]
                {
                    new { text = m.Content ?? string.Empty },
                },
            })
            .ToList();

        var payload = new
        {
            system_instruction = new
            {
                parts = new[]
                {
                    new { text = system ?? string.Empty },
                },
            },
            contents,
            generationConfig = new
            {
                temperature,
                maxOutputTokens = opts.MaxTokens,
            },
        };

        var (response, failureMessage) = await SendRequestAsync(payload, opts, model, ct).ConfigureAwait(false);

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
        string model,
        CancellationToken ct)
    {
        try
        {
            var requestUri = string.Format(
                CultureInfo.InvariantCulture,
                "/v1beta/models/{0}:streamGenerateContent?alt=sse",
                Uri.EscapeDataString(model));

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.TryAddWithoutValidation("x-goog-api-key", opts.ApiKey);
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

                if (!TryParseSseEvent(payloadText, out var delta, out var notice, out var stop))
                {
                    continue;
                }
                if (!string.IsNullOrEmpty(delta)) yield return delta!;
                if (!string.IsNullOrEmpty(notice)) yield return notice!;
                if (stop) yield break;
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

    private static bool TryParseSseEvent(
        string payloadText,
        out string? delta,
        out string? notice,
        out bool stop)
    {
        delta = null;
        notice = null;
        stop = false;
        try
        {
            using var doc = JsonDocument.Parse(payloadText);
            var root = doc.RootElement;

            if (!root.TryGetProperty("candidates", out var candidates))
            {
                if (TryCreatePromptFeedbackNotice(root, out notice))
                {
                    stop = true;
                    return true;
                }
                return false;
            }

            if (candidates.ValueKind != JsonValueKind.Array
                || candidates.GetArrayLength() == 0)
            {
                notice = CreateNoCandidateNotice(root);
                stop = true;
                return true;
            }

            var candidate = candidates[0];
            var sb = new StringBuilder();

            if (candidate.TryGetProperty("content", out var content)
                && content.TryGetProperty("parts", out var parts)
                && parts.ValueKind == JsonValueKind.Array)
            {
                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("text", out var textProp)
                        && textProp.ValueKind == JsonValueKind.String)
                    {
                        sb.Append(textProp.GetString());
                    }
                }
            }

            if (sb.Length > 0)
            {
                delta = sb.ToString();
            }

            if (candidate.TryGetProperty("finishReason", out var finishProp)
                && finishProp.ValueKind == JsonValueKind.String)
            {
                var reason = finishProp.GetString();
                if (!string.IsNullOrEmpty(reason)
                    && !string.Equals(reason, "FINISH_REASON_UNSPECIFIED", StringComparison.Ordinal))
                {
                    notice = CreateFinishReasonNotice(reason);
                    stop = true;
                }
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string? CreateFinishReasonNotice(string reason)
        => reason switch
        {
            "STOP" => null,
            "MAX_TOKENS" =>
                "[Gemini đã dừng vì chạm giới hạn MAX_TOKENS. Hãy yêu cầu trợ lý tiếp tục phần còn lại.]",
            "SAFETY" =>
                "[Gemini đã dừng phản hồi vì bộ lọc an toàn (SAFETY). Hãy diễn đạt lại yêu cầu và không nhập dữ liệu nhạy cảm.]",
            _ => string.Format(
                CultureInfo.InvariantCulture,
                "[Gemini đã dừng phản hồi: {0}. Hãy diễn đạt lại yêu cầu.]",
                reason),
        };

    private static string CreateNoCandidateNotice(JsonElement root)
    {
        if (TryCreatePromptFeedbackNotice(root, out var notice))
        {
            return notice!;
        }

        return "[Gemini không trả về phương án phản hồi. Hãy thử diễn đạt lại yêu cầu.]";
    }

    private static bool TryCreatePromptFeedbackNotice(JsonElement root, out string? notice)
    {
        notice = null;
        if (!root.TryGetProperty("promptFeedback", out var feedback)
            || !feedback.TryGetProperty("blockReason", out var reasonProp)
            || reasonProp.ValueKind != JsonValueKind.String
            || reasonProp.GetString() is not { Length: > 0 } reason)
        {
            return false;
        }

        notice = string.Format(
            CultureInfo.InvariantCulture,
            "[Gemini không trả về phương án phản hồi vì câu hỏi bị chặn ({0}). Hãy diễn đạt lại yêu cầu.]",
            reason);
        return true;
    }
}
