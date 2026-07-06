using System.Text.Json;

namespace TelemedicineLandingPage.Services.Admin;

public static class ProcedureVersionSummaryFormatter
{
    public static string Display(string? summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
            return "—";

        var trimmed = summary.Trim();
        if (!trimmed.StartsWith('{'))
            return trimmed;

        try
        {
            using var document = JsonDocument.Parse(trimmed);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return trimmed;

            if (document.RootElement.TryGetProperty("note", out var note) &&
                note.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(note.GetString()))
            {
                return note.GetString()!;
            }

            if (document.RootElement.TryGetProperty("seed", out var seed) &&
                seed.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(seed.GetString()))
            {
                return $"Dữ liệu mẫu ({seed.GetString()})";
            }

            if (document.RootElement.TryGetProperty("ocrStatus", out var ocrStatus) &&
                ocrStatus.ValueKind == JsonValueKind.String &&
                string.Equals(ocrStatus.GetString(), "OCR_PENDING", StringComparison.OrdinalIgnoreCase))
            {
                return "Đang chờ đối chiếu OCR";
            }
        }
        catch (JsonException)
        {
            return trimmed;
        }

        return trimmed;
    }

    public static string FormatDiffValue(string label, string? value)
    {
        var normalized = NormalizeDisplayText(value);
        if (string.Equals(label, "Tóm tắt", StringComparison.Ordinal))
            return Display(normalized == "Không có" ? null : normalized);

        return normalized;
    }

    public static string NormalizeDisplayText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Không có";

        var trimmed = value.Trim();
        if (string.Equals(trimmed, "Khong co", StringComparison.OrdinalIgnoreCase))
            return "Không có";

        return trimmed;
    }

    public static string ToStorageJson(string? note)
    {
        var value = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        return JsonSerializer.Serialize(new { note = value ?? "Khởi tạo quy trình" });
    }

    public static string FormatVersionSummary(IEnumerable<TelemedicineLandingPage.Models.Admin.Sql.ProcedureVersion> versions)
    {
        var list = versions.OrderByDescending(v => v.VersionNo).ToList();
        if (list.Count == 0)
            return "—";

        var parts = new List<string>();
        var active = list.FirstOrDefault(v => string.Equals(v.StatusCode, "active", StringComparison.OrdinalIgnoreCase));
        if (active is not null)
        {
            parts.Add($"{Label(active)} (hiệu lực)");
        }

        foreach (var status in new[] { "pending_approval", "draft" })
        {
            var version = list.FirstOrDefault(v => string.Equals(v.StatusCode, status, StringComparison.OrdinalIgnoreCase));
            if (version is null || version.ProcedureVersionId == active?.ProcedureVersionId)
                continue;

            parts.Add($"{Label(version)} ({StatusShort(status)})");
            break;
        }

        if (parts.Count == 0)
        {
            var latest = list[0];
            parts.Add($"{Label(latest)} ({StatusShort(latest.StatusCode)})");
        }

        return string.Join(" • ", parts);
    }

    private static string Label(TelemedicineLandingPage.Models.Admin.Sql.ProcedureVersion version)
        => version.VersionLabel ?? $"v{version.VersionNo}";

    private static string StatusShort(string status) => status switch
    {
        "pending_approval" => "chờ duyệt",
        "draft" => "nháp",
        "superseded" => "đã thay thế",
        "archived" => "lưu trữ",
        "rejected" => "trả lại",
        "active" => "hiệu lực",
        _ => status
    };
}
