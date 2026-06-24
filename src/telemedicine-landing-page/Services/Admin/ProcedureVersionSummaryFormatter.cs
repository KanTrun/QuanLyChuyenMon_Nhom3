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

    public static string ToStorageJson(string? note)
    {
        var value = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        return JsonSerializer.Serialize(new { note = value ?? "Khởi tạo quy trình" });
    }
}
