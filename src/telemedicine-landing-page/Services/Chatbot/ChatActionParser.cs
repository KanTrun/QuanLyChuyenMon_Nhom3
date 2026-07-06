using TelemedicineLandingPage.Models.Chatbot;

namespace TelemedicineLandingPage.Services.Chatbot;

public static class ChatActionParser
{
    private static readonly HashSet<string> AllowedRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        "/admin/lam-sang",
        "/qlcm/lam-sang",
        "/admin/phac-do",
        "/admin/quy-trinh",
        "/admin/quy-trinh/tao"
    };

    public static IReadOnlyCollection<string> Routes => AllowedRoutes;

    public static bool IsAllowedRoute(string route)
        => AllowedRoutes.Contains(NormalizeRoute(route));

    public static bool TryCreate(
        ChatActionKind kind,
        string label,
        string route,
        string? draftNonce,
        out ChatAction action)
    {
        var normalizedRoute = NormalizeRoute(route);
        if (!AllowedRoutes.Contains(normalizedRoute))
        {
            action = new ChatAction(kind, label, string.Empty, null);
            return false;
        }

        if (kind == ChatActionKind.NavigateWithDraft && string.IsNullOrWhiteSpace(draftNonce))
        {
            action = new ChatAction(kind, label, string.Empty, null);
            return false;
        }

        action = new ChatAction(
            kind,
            string.IsNullOrWhiteSpace(label) ? "Open" : label.Trim(),
            normalizedRoute,
            kind == ChatActionKind.NavigateWithDraft ? draftNonce : null);
        return true;
    }

    private static string NormalizeRoute(string route)
    {
        if (string.IsNullOrWhiteSpace(route)) return string.Empty;
        var trimmed = route.Trim();
        if (!trimmed.StartsWith('/')) trimmed = "/" + trimmed;
        return trimmed.Split('?', '#')[0];
    }
}
