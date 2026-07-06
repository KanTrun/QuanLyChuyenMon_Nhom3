using Microsoft.AspNetCore.Components.Rendering;

namespace TelemedicineLandingPage.Components.Admin;

/// <summary>
/// Inline SVG icon library used by the admin shell. All icons share the same
/// 24x24 viewBox, stroke 1.5, currentColor, round line caps so they can be
/// recoloured by their parent via CSS.
/// </summary>
internal static class AdminIcons
{
    public static void Render(RenderTreeBuilder builder, string name)
    {
        var path = ResolvePath(name);
        builder.OpenElement(0, "svg");
        builder.AddAttribute(1, "viewBox", "0 0 24 24");
        builder.AddAttribute(2, "width", "18");
        builder.AddAttribute(3, "height", "18");
        builder.AddAttribute(4, "fill", "none");
        builder.AddAttribute(5, "stroke", "currentColor");
        builder.AddAttribute(6, "stroke-width", "1.5");
        builder.AddAttribute(7, "stroke-linecap", "round");
        builder.AddAttribute(8, "stroke-linejoin", "round");
        builder.AddAttribute(9, "aria-hidden", "true");
        builder.AddAttribute(10, "focusable", "false");
        builder.OpenElement(11, "path");
        builder.AddAttribute(12, "d", path);
        builder.CloseElement();
        builder.CloseElement();
    }

    private static string ResolvePath(string name) => name switch
    {
        "dashboard" => "M3 13h7V3H3v10Zm0 8h7v-6H3v6Zm11 0h7V11h-7v10Zm0-18v6h7V3h-7Z",
        "workflow" => "M4 6h6v6H4V6Zm10 6h6v6h-6v-6ZM7 12v3a3 3 0 0 0 3 3h4M14 6h2a4 4 0 0 1 4 4",
        "list" => "M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01",
        "plus" => "M12 5v14M5 12h14",
        "check" => "m4 12 5 5 11-12",
        "shield" => "M12 3 4 6v6c0 4.5 3.4 7.5 8 9 4.6-1.5 8-4.5 8-9V6l-8-3Zm-3 9 2 2 4-4",
        "catalog" => "M4 5h6v6H4V5Zm10 0h6v6h-6V5ZM4 13h6v6H4v-6Zm10 0h6v6h-6v-6Z",
        "stethoscope" => "M5 4v5a4 4 0 0 0 8 0V4M9 14v2a5 5 0 0 0 10 0v-2M19 14a2 2 0 1 0 0-4 2 2 0 0 0 0 4Z",
        "chart" => "M4 19V5M4 19h16M8 16v-5M12 16V8M16 16v-3",
        "package" => "M3 7 12 3l9 4M3 7v10l9 4 9-4V7M3 7l9 4 9-4M12 11v10",
        "heart" => "M12 21s-7-4.4-7-10a4.5 4.5 0 0 1 8-2.7A4.5 4.5 0 0 1 19 11c0 5.6-7 10-7 10Z",
        "settings" => "M12 9a3 3 0 1 0 0 6 3 3 0 0 0 0-6Zm8.4 3a8.4 8.4 0 0 0-.1-1.4l2-1.5-2-3.4-2.4.9a8.5 8.5 0 0 0-2.4-1.4L15 2h-4l-.5 2.6A8.5 8.5 0 0 0 8.1 6l-2.4-.9-2 3.4 2 1.5a8.4 8.4 0 0 0 0 2.8l-2 1.5 2 3.4 2.4-.9a8.5 8.5 0 0 0 2.4 1.4L11 22h4l.5-2.6a8.5 8.5 0 0 0 2.4-1.4l2.4.9 2-3.4-2-1.5c0-.5.1-.9.1-1.4Z",
        "bell" => "M6 8a6 6 0 1 1 12 0c0 7 3 7 3 9H3c0-2 3-2 3-9Zm4 13a2 2 0 0 0 4 0",
        "search" => "M11 19a8 8 0 1 1 0-16 8 8 0 0 1 0 16Zm10 2-4.3-4.3",
        "menu" => "M3 6h18M3 12h18M3 18h18",
        "moon" => "M21 12.8A9 9 0 1 1 11.2 3 7 7 0 0 0 21 12.8Z",
        "sun" => "M12 4V2M12 22v-2M4 12H2M22 12h-2M5.6 5.6 4.2 4.2M19.8 19.8l-1.4-1.4M5.6 18.4l-1.4 1.4M19.8 4.2l-1.4 1.4M12 8a4 4 0 1 0 0 8 4 4 0 0 0 0-8Z",
        "fullscreen" => "M4 9V4h5M20 9V4h-5M4 15v5h5M20 15v5h-5",
        "fullscreen-exit" => "M9 4v5H4M15 4v5h5M9 20v-5H4M15 20v-5h5",
        "user" => "M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8Zm-7 9a7 7 0 0 1 14 0",
        "chevron-down" => "m6 9 6 6 6-6",
        "logout" => "M15 12H4M9 7l-5 5 5 5M14 4h5a1 1 0 0 1 1 1v14a1 1 0 0 1-1 1h-5",
        "spark" => "M12 2v4M12 18v4M4 12H2M22 12h-2M5.6 5.6 4.2 4.2M19.8 19.8l-1.4-1.4M5.6 18.4l-1.4 1.4M19.8 4.2l-1.4 1.4",
        "trend-up" => "M3 17 9 11l4 4 8-8M14 7h7v7",
        "trend-down" => "M3 7 9 13l4-4 8 8M14 17h7v-7",
        "command" => "M9 6a3 3 0 1 0-3 3h12a3 3 0 1 0-3-3v12a3 3 0 1 0 3-3H6a3 3 0 1 0 3 3V6Z",
        "history" => "M3 12a9 9 0 1 0 3-6.7L3 8M3 3v5h5M12 7v5l3 2",
        "team" => "M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8Zm0 2a7 7 0 0 0-7 7h14M16 11a3 3 0 1 0 0-6M22 20a6 6 0 0 0-5-5.9",
        "alert-dot" => "M12 8v4m0 4h.01M12 22a10 10 0 1 1 0-20 10 10 0 0 1 0 20Z",
        _ => "M12 4v16M4 12h16",
    };
}
