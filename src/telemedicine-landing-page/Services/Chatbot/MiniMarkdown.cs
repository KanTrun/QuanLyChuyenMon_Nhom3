using System.Text;
using System.Text.RegularExpressions;

namespace TelemedicineLandingPage.Services.Chatbot;

/// <summary>
/// Tiny markdown-lite renderer used for assistant transcripts. The renderer
/// HTML-escapes every input first, then applies a small set of safe regex-based
/// rules so that <c>**bold**</c>, <c>*italic*</c>, <c>`code`</c>, bullet lists
/// (<c>- item</c>) and explicit newlines render correctly. Heavy lifting lives
/// in two passes: a per-line pass that detects bullet lists and a simple
/// replacement pass for inline emphasis. Keep this file under 100 lines so it
/// stays auditable.
/// </summary>
public static class MiniMarkdown
{
    private static readonly Regex BoldPattern =
        new(@"\*\*(?<inner>[^\*\n]+?)\*\*", RegexOptions.Compiled);

    private static readonly Regex ItalicPattern =
        new(@"\*(?<inner>[^\*\n]+?)\*", RegexOptions.Compiled);

    private static readonly Regex CodePattern =
        new("`(?<inner>[^`\n]+?)`", RegexOptions.Compiled);

    /// <summary>Render the supplied markdown-lite source as a sanitized HTML string.</summary>
    public static string ToHtml(string? source)
    {
        if (string.IsNullOrEmpty(source)) return string.Empty;

        // Step 1: escape ONLY the five HTML-special characters. We deliberately
        // avoid WebUtility.HtmlEncode because it would convert Vietnamese
        // diacritics into numeric entities (Đ -> &#272;), making the rendered
        // bubble visually correct but breaking string-level assertions and
        // accessibility tooling.
        var escaped = EscapeHtml(source);

        // Step 2: walk lines and group runs of "- " bullet lines into <ul>...
        var rawLines = escaped.Replace("\r\n", "\n").Split('\n');
        var output = new StringBuilder(escaped.Length + 32);
        var inList = false;

        foreach (var line in rawLines)
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("- ", StringComparison.Ordinal))
            {
                if (!inList)
                {
                    output.Append("<ul>");
                    inList = true;
                }
                var item = ApplyInline(trimmed[2..]);
                output.Append("<li>").Append(item).Append("</li>");
                continue;
            }

            if (inList)
            {
                output.Append("</ul>");
                inList = false;
            }

            output.Append(ApplyInline(line));
            output.Append("<br/>");
        }

        if (inList)
        {
            output.Append("</ul>");
        }

        // Trim a trailing <br/> so messages don't gain a phantom newline.
        if (output.Length >= 5 && output.ToString(output.Length - 5, 5) == "<br/>")
        {
            output.Length -= 5;
        }

        return output.ToString();
    }

    private static string EscapeHtml(string source)
    {
        var builder = new StringBuilder(source.Length);
        foreach (var ch in source)
        {
            switch (ch)
            {
                case '&': builder.Append("&amp;"); break;
                case '<': builder.Append("&lt;"); break;
                case '>': builder.Append("&gt;"); break;
                case '"': builder.Append("&quot;"); break;
                case '\'': builder.Append("&#39;"); break;
                default: builder.Append(ch); break;
            }
        }
        return builder.ToString();
    }

    private static string ApplyInline(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var result = BoldPattern.Replace(input, "<strong>${inner}</strong>");
        result = ItalicPattern.Replace(result, "<em>${inner}</em>");
        result = CodePattern.Replace(result, "<code>${inner}</code>");
        return result;
    }
}
