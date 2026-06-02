using System.Text;
using TelemedicineLandingPage.Models.Admin;
using TelemedicineLandingPage.Models.Chatbot;
using TelemedicineLandingPage.Services.Admin;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Services.Chatbot;

public sealed class QlcmChatbotContextBuilder : IChatbotContextBuilder
{
    private readonly IAdminNavigationState _navigation;
    private readonly IMedDataStore _store;
    private readonly NavGate _gate;
    private readonly IChatbotPrivacyGuard _privacyGuard;

    public QlcmChatbotContextBuilder(
        IAdminNavigationState navigation,
        IMedDataStore store,
        NavGate gate,
        IChatbotPrivacyGuard privacyGuard)
    {
        _navigation = navigation;
        _store = store;
        _gate = gate;
        _privacyGuard = privacyGuard;
    }

    public string BuildSystemPrompt(
        IReadOnlyList<ChatMessage> conversation,
        string? configuredPrompt,
        string? customizationPrompt)
    {
        var builder = new StringBuilder();
        AppendOptional(builder, "Hướng dẫn triển khai", configuredPrompt);
        AppendSafeCustomization(builder, customizationPrompt);
        builder.AppendLine().AppendLine(QlcmChatbotKnowledgeCatalog.CorePrompt);

        var question = conversation.LastOrDefault(message => message.Role == ChatRole.User)?.Content;
        var topics = QlcmChatbotKnowledgeCatalog.FindRelevant(question);
        if (topics.Count > 0)
        {
            builder.AppendLine().AppendLine("Ngữ cảnh nghiệp vụ liên quan:");
            foreach (var topic in topics)
            {
                builder.Append("- ").AppendLine(topic.Context);
            }
        }

        builder.AppendLine().AppendLine("Route người dùng hiện tại được phép mở:");
        var routes = Flatten(_gate.Filter(_navigation.NavItems)).Take(32).ToList();
        if (routes.Count == 0)
        {
            builder.AppendLine("- Không có route nghiệp vụ khả dụng.");
        }
        else
        {
            foreach (var route in routes)
            {
                builder.Append("- ").Append(route.Label).Append(": ").AppendLine(route.Url);
            }
        }

        builder.AppendLine().AppendLine("Snapshot tổng hợp không định danh:");
        builder.Append("- Quy trình: ").AppendLine(_store.Procedures.Count.ToString());
        builder.Append("- Phiên bản quy trình active: ")
            .AppendLine(_store.ProcedureVersions.Count(version => version.StatusCode == "active").ToString());
        builder.Append("- Dịch vụ kỹ thuật: ").AppendLine(_store.TechnicalServices.Count.ToString());
        builder.Append("- Phiên bản phác đồ active: ")
            .AppendLine(_store.ClinicalProtocolVersions.Count(version => version.StatusCode == "active").ToString());
        builder.Append("- Yêu cầu đổi quyền đang lên lịch: ")
            .AppendLine(_store.PermissionChangeRequests.Count(request => request.ChangeStatus == "scheduled").ToString());
        builder.AppendLine("Không có dữ liệu bệnh nhân, lượt khám, nội dung thông báo hoặc audit payload trong snapshot.");
        return builder.ToString();
    }

    private static IEnumerable<AdminNavItem> Flatten(IEnumerable<AdminNavItem> items)
    {
        foreach (var item in items)
        {
            if (item.Children is { Count: > 0 })
            {
                foreach (var child in Flatten(item.Children))
                {
                    yield return child;
                }
            }
            else
            {
                yield return item;
            }
        }
    }

    private static void AppendOptional(StringBuilder builder, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            builder.AppendLine().Append(label).Append(": ").AppendLine(value.Trim());
        }
    }

    private void AppendSafeCustomization(StringBuilder builder, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && _privacyGuard.CanSend(value, out _))
        {
            AppendOptional(builder, "Tùy chỉnh phiên hiện tại, không được mâu thuẫn quy tắc bắt buộc", value);
        }
    }
}

public sealed class CoreOnlyChatbotContextBuilder : IChatbotContextBuilder
{
    public string BuildSystemPrompt(
        IReadOnlyList<ChatMessage> conversation,
        string? configuredPrompt,
        string? customizationPrompt)
    {
        var builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(configuredPrompt))
        {
            builder.AppendLine().Append("Hướng dẫn triển khai: ").AppendLine(configuredPrompt.Trim());
        }
        if (!string.IsNullOrWhiteSpace(customizationPrompt) &&
            new ChatbotPrivacyGuard().CanSend(customizationPrompt, out _))
        {
            builder.AppendLine()
                .Append("Tùy chỉnh phiên hiện tại, không được mâu thuẫn quy tắc bắt buộc: ")
                .AppendLine(customizationPrompt.Trim());
        }
        builder.AppendLine().AppendLine(QlcmChatbotKnowledgeCatalog.CorePrompt);
        return builder.ToString();
    }
}
