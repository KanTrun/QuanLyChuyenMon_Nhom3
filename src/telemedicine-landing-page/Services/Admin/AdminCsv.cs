using System.Text;

namespace TelemedicineLandingPage.Services.Admin;

/// <summary>
/// Tiny RFC 4180 compatible CSV helper used by the Danh mục import / export flow.
/// Handles double-quoted fields, embedded commas, line breaks and escaped quotes
/// so the export -> import round-trip preserves rows verbatim.
/// </summary>
internal static class AdminCsv
{
    public static string Encode(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var needsQuote = value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
        if (!needsQuote) return value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    public static IReadOnlyList<IReadOnlyList<string>> Parse(string csv)
    {
        var rows = new List<IReadOnlyList<string>>();
        if (string.IsNullOrEmpty(csv)) return rows;

        var current = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < csv.Length; i++)
        {
            var c = csv[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < csv.Length && csv[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(c);
                }
            }
            else
            {
                switch (c)
                {
                    case '"':
                        inQuotes = true;
                        break;
                    case ',':
                        current.Add(field.ToString());
                        field.Clear();
                        break;
                    case '\r':
                        // Treat as part of the line break only when followed by \n.
                        if (i + 1 < csv.Length && csv[i + 1] == '\n')
                        {
                            i++;
                        }
                        current.Add(field.ToString());
                        field.Clear();
                        rows.Add(current);
                        current = new List<string>();
                        break;
                    case '\n':
                        current.Add(field.ToString());
                        field.Clear();
                        rows.Add(current);
                        current = new List<string>();
                        break;
                    default:
                        field.Append(c);
                        break;
                }
            }
        }

        // Flush trailing field/row if the file does not end with a newline.
        if (field.Length > 0 || current.Count > 0)
        {
            current.Add(field.ToString());
            rows.Add(current);
        }

        return rows;
    }
}
