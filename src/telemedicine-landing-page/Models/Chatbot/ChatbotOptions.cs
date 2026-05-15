namespace TelemedicineLandingPage.Models.Chatbot;

/// <summary>
/// Strongly-typed binding for the appsettings.json "Chatbot" section. The defaults
/// match the values shipped in appsettings.json so that an empty configuration
/// section still produces a working demo client.
/// </summary>
public sealed class ChatbotOptions
{
    public const string SectionName = "Chatbot";

    public string Provider { get; set; } = "Anthropic";
    public string Model { get; set; } = "claude-sonnet-4-5-20250929";
    public string BaseUrl { get; set; } = "https://api.anthropic.com";
    public string ApiKey { get; set; } = string.Empty;
    public int MaxTokens { get; set; } = 1024;
    public string AnthropicVersion { get; set; } = "2023-06-01";
    public int RequestTimeoutSeconds { get; set; } = 90;

    public string SystemPrompt { get; set; } =
        "Bạn là trợ lý AI nội bộ của ứng dụng QLCM Pro - phần mềm quản lý chuyên môn của bệnh viện. " +
        "Hãy trả lời ngắn gọn, chính xác bằng tiếng Việt có dấu, ưu tiên các bước cụ thể có thể thao tác " +
        "trong ứng dụng (Quy trình kỹ thuật, Phân quyền, Danh mục, Phác đồ, Báo cáo, Cài đặt). " +
        "Khi không chắc chắn, hãy nói rõ và đề xuất cách kiểm tra.";
}
