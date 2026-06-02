using TelemedicineLandingPage.Models.Chatbot;

namespace TelemedicineLandingPage.Services.Chatbot;

public interface IChatbotContextBuilder
{
    string BuildSystemPrompt(
        IReadOnlyList<ChatMessage> conversation,
        string? configuredPrompt,
        string? customizationPrompt);
}
