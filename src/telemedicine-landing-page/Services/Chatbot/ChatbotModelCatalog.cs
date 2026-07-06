namespace TelemedicineLandingPage.Services.Chatbot;

public sealed record ChatbotModelOption(string Value, string Label);

public static class ChatbotModelCatalog
{
    private static readonly IReadOnlyList<ChatbotModelOption> GeminiModels =
    [
        new("gemini-2.5-flash", "Gemini 2.5 Flash"),
        new("gemini-2.5-flash-lite", "Gemini 2.5 Flash Lite"),
        new("gemini-2.5-pro", "Gemini 2.5 Pro")
    ];

    private static readonly IReadOnlyList<ChatbotModelOption> AnthropicModels =
    [
        new("claude-sonnet-4-5-20250929", "Claude Sonnet 4.5")
    ];

    public static IReadOnlyList<ChatbotModelOption> ForProvider(string? provider)
        => string.Equals(provider, "Anthropic", StringComparison.OrdinalIgnoreCase)
            ? AnthropicModels
            : GeminiModels;

    public static bool IsKnownProvider(string? provider)
        => string.Equals(provider, "Gemini", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(provider, "Anthropic", StringComparison.OrdinalIgnoreCase);

    public static bool IsSupportedModel(string? provider, string? model)
        => IsKnownProvider(provider) &&
           ForProvider(provider).Any(option => string.Equals(option.Value, model, StringComparison.OrdinalIgnoreCase));

    public static bool IsAllowedBaseUrl(string? provider, string? baseUrl)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        var expectedHost = string.Equals(provider, "Anthropic", StringComparison.OrdinalIgnoreCase)
            ? "api.anthropic.com"
            : "generativelanguage.googleapis.com";
        return IsKnownProvider(provider) &&
               string.Equals(uri.Host, expectedHost, StringComparison.OrdinalIgnoreCase);
    }

    public static string Resolve(string? provider, string? configuredModel, string? preferredModel)
    {
        var supported = ForProvider(provider);
        if (supported.Any(model => string.Equals(model.Value, preferredModel, StringComparison.OrdinalIgnoreCase)))
        {
            return preferredModel!;
        }

        if (supported.Any(model => string.Equals(model.Value, configuredModel, StringComparison.OrdinalIgnoreCase)))
        {
            return configuredModel!;
        }

        return supported[0].Value;
    }
}
