using Microsoft.Extensions.Configuration;
using TelemedicineLandingPage.Models.Chatbot;

namespace telemedicine_landing_page.tests.Chatbot;

public class ChatbotOptionsTests
{
    [Fact]
    public void BindsFromConfiguration_PopulatesAllFields()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Chatbot:Provider"] = "Anthropic",
                ["Chatbot:Model"] = "claude-sonnet-4-5-20250929",
                ["Chatbot:BaseUrl"] = "https://api.anthropic.com",
                ["Chatbot:ApiKey"] = "sk-test-key",
                ["Chatbot:MaxTokens"] = "1500",
                ["Chatbot:AnthropicVersion"] = "2023-06-01",
                ["Chatbot:RequestTimeoutSeconds"] = "120",
                ["Chatbot:SystemPrompt"] = "Trợ lý nội bộ.",
            })
            .Build();

        var opts = new ChatbotOptions();
        config.GetSection(ChatbotOptions.SectionName).Bind(opts);

        Assert.Equal("Anthropic", opts.Provider);
        Assert.Equal("claude-sonnet-4-5-20250929", opts.Model);
        Assert.Equal("https://api.anthropic.com", opts.BaseUrl);
        Assert.Equal("sk-test-key", opts.ApiKey);
        Assert.Equal(1500, opts.MaxTokens);
        Assert.Equal("2023-06-01", opts.AnthropicVersion);
        Assert.Equal(120, opts.RequestTimeoutSeconds);
        Assert.Equal("Trợ lý nội bộ.", opts.SystemPrompt);
    }

    [Fact]
    public void DefaultsAreSafeWhenSectionMissing()
    {
        var opts = new ChatbotOptions();

        Assert.Equal("Gemini", opts.Provider);
        Assert.Equal("gemini-2.5-flash", opts.Model);
        Assert.Equal("https://generativelanguage.googleapis.com", opts.BaseUrl);
        Assert.Equal(string.Empty, opts.ApiKey);
        Assert.Equal(4096, opts.MaxTokens);
        Assert.Equal("2023-06-01", opts.AnthropicVersion);
        Assert.Equal(90, opts.RequestTimeoutSeconds);
        Assert.False(string.IsNullOrWhiteSpace(opts.SystemPrompt));
        Assert.Contains("QLCM", opts.SystemPrompt);
    }
}
