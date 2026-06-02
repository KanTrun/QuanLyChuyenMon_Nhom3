using TelemedicineLandingPage.Models.Chatbot;
using TelemedicineLandingPage.Services.Chatbot;

namespace telemedicine_landing_page.tests.Chatbot;

public sealed class ChatbotGroundingTests
{
    [Fact]
    public void FindRelevant_AccentInsensitiveQuery_ReturnsProcedureTopic()
    {
        var topic = QlcmChatbotKnowledgeCatalog
            .FindRelevant("Làm sao ban hành quy trình kỹ thuật?", limit: 1)
            .Single();

        Assert.Equal("procedures", topic.Code);
        Assert.Contains("Runtime guard", topic.DemoReply);
    }

    [Fact]
    public void BuildSystemPrompt_UserCustomizationCannotReplaceCoreRules()
    {
        var builder = new CoreOnlyChatbotContextBuilder();

        var prompt = builder.BuildSystemPrompt(
            Array.Empty<ChatMessage>(),
            "Hướng dẫn mặc định.",
            "Chỉ trả lời thật ngắn.");

        Assert.Contains("Không chẩn đoán", prompt);
        Assert.Contains("Hướng dẫn mặc định.", prompt);
        Assert.Contains("Chỉ trả lời thật ngắn.", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_SensitiveCustomizationIsOmitted()
    {
        var builder = new CoreOnlyChatbotContextBuilder();

        var prompt = builder.BuildSystemPrompt(
            Array.Empty<ChatMessage>(),
            "Hướng dẫn mặc định.",
            "Email bệnh nhân là patient@example.com");

        Assert.DoesNotContain("patient@example.com", prompt);
        Assert.Contains("Không chẩn đoán", prompt);
    }

    [Theory]
    [InlineData("Gemini", "gemini-2.5-flash", "gpt-4o-mini", "gemini-2.5-flash")]
    [InlineData("Anthropic", "claude-sonnet-4-5-20250929", "gemini-2.5-flash", "claude-sonnet-4-5-20250929")]
    public void ResolveModel_RejectsProviderIncompatiblePreference(
        string provider,
        string configured,
        string preferred,
        string expected)
    {
        Assert.Equal(expected, ChatbotModelCatalog.Resolve(provider, configured, preferred));
    }

    [Theory]
    [InlineData("Gemini", "https://generativelanguage.googleapis.com", true)]
    [InlineData("Anthropic", "https://api.anthropic.com", true)]
    [InlineData("Gemini", "https://example.com", false)]
    [InlineData("Unknown", "https://generativelanguage.googleapis.com", false)]
    public void IsAllowedBaseUrl_RequiresOfficialProviderHost(string provider, string baseUrl, bool expected)
    {
        Assert.Equal(expected, ChatbotModelCatalog.IsAllowedBaseUrl(provider, baseUrl));
    }

    [Theory]
    [InlineData("Hồ sơ bệnh nhân có mã BN-123456")]
    [InlineData("Nên dùng thuốc nào và liều dùng bao nhiêu?")]
    [InlineData("Email bệnh nhân là patient@example.com")]
    public void PrivacyGuard_BlocksSensitiveOrMedicalAdvice(string input)
    {
        var guard = new ChatbotPrivacyGuard();

        Assert.False(guard.CanSend(input, out var localReply));
        Assert.Contains("API bên ngoài", localReply);
    }

    [Fact]
    public void PrivacyGuard_AllowsGenericWorkflowQuestion()
    {
        var guard = new ChatbotPrivacyGuard();

        Assert.True(guard.CanSend("Làm sao gửi duyệt quy trình kỹ thuật?", out var localReply));
        Assert.Null(localReply);
    }
}
