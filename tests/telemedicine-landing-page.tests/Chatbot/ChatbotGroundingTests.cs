using TelemedicineLandingPage.Models.Chatbot;
using TelemedicineLandingPage.Services.Chatbot;

namespace telemedicine_landing_page.tests.Chatbot;

public sealed class ChatbotGroundingTests
{
    [Fact]
    public void FindRelevant_AccentInsensitiveQuery_ReturnsProcedureTopic()
    {
        var topic = QlcmChatbotKnowledgeCatalog
            .FindRelevant("lam sao ban hanh quy trinh ky thuat", limit: 1)
            .Single();

        Assert.Equal("procedures", topic.Code);
        Assert.Contains("Runtime guard", topic.DemoReply);
    }

    [Fact]
    public void FindRelevant_SignatureTopic_ExplainsInternalSignature()
    {
        var topic = QlcmChatbotKnowledgeCatalog
            .FindRelevant("ky xac nhan ho so noi bo", limit: 1)
            .Single();

        Assert.Equal("signatures", topic.Code);
        Assert.Contains("nội bộ", topic.DemoReply);
    }

    [Fact]
    public void IsProjectScoped_OnlyAllowsQlcmQuestions()
    {
        Assert.True(QlcmChatbotKnowledgeCatalog.IsProjectScoped("cách duyệt tài khoản người dùng"));
        Assert.False(QlcmChatbotKnowledgeCatalog.IsProjectScoped("hôm nay thời tiết thế nào"));
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
        Assert.Contains("không hiển thị route kỹ thuật", prompt, StringComparison.OrdinalIgnoreCase);
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
    [InlineData("Ho so benh nhan co ma BN-123456")]
    [InlineData("Nen dung thuoc nao va lieu dung bao nhieu?")]
    [InlineData("Email benh nhan la patient@example.com")]
    public void PrivacyGuard_BlocksSensitiveOrMedicalAdvice(string input)
    {
        var guard = new ChatbotPrivacyGuard();

        Assert.False(guard.CanSend(input, out var localReply));
        Assert.NotNull(localReply);
        Assert.Contains("API", localReply);
    }

    [Fact]
    public void PrivacyGuard_AllowsGenericWorkflowQuestion()
    {
        var guard = new ChatbotPrivacyGuard();

        Assert.True(guard.CanSend("lam sao gui duyet quy trinh ky thuat", out var localReply));
        Assert.Null(localReply);
    }
}
