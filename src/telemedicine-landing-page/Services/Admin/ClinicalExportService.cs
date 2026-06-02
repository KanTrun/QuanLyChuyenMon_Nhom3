using System.Net;
using System.Text;
using TelemedicineLandingPage.Models.Admin;
using Sql = TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin;

public interface IClinicalExportService
{
    string BuildWorkspaceHtmlReport(DateTime generatedAtUtc);
}

public sealed class ClinicalExportService(IMedDataStore store) : IClinicalExportService
{
    public string BuildWorkspaceHtmlReport(DateTime generatedAtUtc)
    {
        var patients = store.PatientRefs.OrderBy(p => p.PatientCode).ToList();
        var html = new StringBuilder();

        html.AppendLine("<!doctype html><html lang=\"vi\"><head><meta charset=\"utf-8\">");
        html.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        html.AppendLine("<title>Bao cao lam sang QLCM Pro</title>");
        html.AppendLine("<style>");
        html.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;margin:32px;color:#172033;background:#fff}");
        html.AppendLine("h1{margin:0 0 8px;font-size:28px}h2{margin:28px 0 8px;font-size:20px}h3{margin:18px 0 8px;font-size:16px}");
        html.AppendLine(".muted{color:#667085}.summary{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:10px;margin:20px 0}");
        html.AppendLine(".card{border:1px solid #d0d5dd;border-radius:8px;padding:12px}.value{font-size:24px;font-weight:700}");
        html.AppendLine("table{width:100%;border-collapse:collapse;margin:8px 0 16px}th,td{border:1px solid #d0d5dd;padding:8px;text-align:left;vertical-align:top}");
        html.AppendLine("th{background:#f2f4f7}.patient{break-inside:avoid;border-top:2px solid #344054;padding-top:14px}.empty{color:#98a2b3;font-style:italic}");
        html.AppendLine("@media print{body{margin:18mm}.summary{grid-template-columns:repeat(2,minmax(0,1fr))}}");
        html.AppendLine("</style></head><body>");
        html.AppendLine("<h1>Bao cao tong hop lam sang</h1>");
        html.AppendLine($"<p class=\"muted\">QLCM Pro - xuat luc {Text(AdminDateTimeDisplay.DateTime(generatedAtUtc))}</p>");
        AppendSummary(html, patients.Count);

        foreach (var patient in patients)
        {
            AppendPatient(html, patient);
        }

        html.AppendLine("</body></html>");
        return html.ToString();
    }

    private void AppendSummary(StringBuilder html, int patientCount)
    {
        html.AppendLine("<section class=\"summary\">");
        AppendSummaryCard(html, "Benh nhan", patientCount);
        AppendSummaryCard(html, "Luot kham", store.EncounterRefs.Count);
        AppendSummaryCard(html, "Phac do da ap dung", store.PatientProtocolApplications.Count);
        AppendSummaryCard(html, "Chi dinh ky thuat", store.TechnicalOrders.Count(o => o.PatientRefId.HasValue));
        html.AppendLine("</section>");
    }

    private static void AppendSummaryCard(StringBuilder html, string label, int value)
        => html.AppendLine($"<div class=\"card\"><div class=\"muted\">{Text(label)}</div><div class=\"value\">{value}</div></div>");

    private void AppendPatient(StringBuilder html, Sql.PatientRef patient)
    {
        html.AppendLine("<section class=\"patient\">");
        html.AppendLine($"<h2>{Text(DisplayPatient(patient))}</h2>");
        html.AppendLine("<table><tbody>");
        AppendRow(html, "Ma benh nhan", patient.PatientCode);
        AppendRow(html, "Ma ngoai", patient.ExternalPatientId);
        AppendRow(html, "Nguon", patient.SourceSystemCode);
        AppendRow(html, "Ngay sinh", patient.BirthDate?.ToString("dd/MM/yyyy"));
        AppendRow(html, "Gioi tinh", Lookup(Sql.MedLookups.Genders, patient.GenderCode));
        AppendRow(html, "Ngay tao", AdminDateTimeDisplay.DateTime(patient.CreatedAt));
        html.AppendLine("</tbody></table>");

        AppendEncounters(html, patient.PatientRefId);
        AppendProtocolApplications(html, patient.PatientRefId);
        AppendTechnicalOrders(html, patient.PatientRefId);
        html.AppendLine("</section>");
    }

    private void AppendEncounters(StringBuilder html, Guid patientId)
    {
        var encounters = store.EncounterRefs
            .Where(e => e.PatientRefId == patientId)
            .OrderByDescending(e => e.StartedAt)
            .ToList();

        html.AppendLine("<h3>Luot kham</h3>");
        AppendTable(html, new[] { "Ma luot kham", "Loai", "Khoa", "Bat dau", "Ket thuc" }, encounters,
            e => new[]
            {
                e.ExternalEncounterId,
                e.EncounterType,
                DepartmentName(e.DepartmentId),
                AdminDateTimeDisplay.DateTime(e.StartedAt),
                AdminDateTimeDisplay.DateTime(e.EndedAt)
            });
    }

    private void AppendProtocolApplications(StringBuilder html, Guid patientId)
    {
        var apps = store.PatientProtocolApplications
            .Where(a => a.PatientRefId == patientId)
            .OrderByDescending(a => a.AppliedAt)
            .ToList();

        html.AppendLine("<h3>Phac do da ap dung</h3>");
        AppendTable(html, new[] { "Phac do", "Luot kham", "Chan doan", "Trang thai", "Thoi diem", "Ly do/ngu canh" }, apps,
            a => new[]
            {
                ProtocolVersionName(a.ClinicalProtocolVersionId),
                EncounterCode(a.EncounterRefId),
                a.DiagnosisCode,
                Lookup(Sql.MedLookups.ProtocolApplicationStatuses, a.ApplicationStatus),
                AdminDateTimeDisplay.DateTime(a.AppliedAt),
                string.IsNullOrWhiteSpace(a.SkippedReason) ? a.DecisionContextJson : a.SkippedReason
            });
    }

    private void AppendTechnicalOrders(StringBuilder html, Guid patientId)
    {
        var orders = store.TechnicalOrders
            .Where(o => o.PatientRefId == patientId)
            .OrderByDescending(o => o.OrderedAt)
            .ToList();

        html.AppendLine("<h3>Chi dinh ky thuat lien quan</h3>");
        AppendTable(html, new[] { "Dich vu", "Luot kham", "Khoa", "Trang thai", "Ngay tao", "Hoan tat" }, orders,
            o => new[]
            {
                ServiceName(o.TechnicalServiceId),
                EncounterCode(o.EncounterRefId),
                DepartmentName(o.OrderingDepartmentId),
                Lookup(Sql.MedLookups.OrderStatuses, o.OrderStatus),
                AdminDateTimeDisplay.DateTime(o.OrderedAt),
                AdminDateTimeDisplay.DateTime(o.CompletedAt)
            });
    }

    private static void AppendTable<T>(StringBuilder html, IReadOnlyList<string> headers, IReadOnlyList<T> rows, Func<T, IReadOnlyList<string?>> cells)
    {
        if (rows.Count == 0)
        {
            html.AppendLine("<p class=\"empty\">Chua co du lieu.</p>");
            return;
        }

        html.Append("<table><thead><tr>");
        foreach (var header in headers) html.Append($"<th>{Text(header)}</th>");
        html.AppendLine("</tr></thead><tbody>");
        foreach (var row in rows)
        {
            html.Append("<tr>");
            foreach (var cell in cells(row)) html.Append($"<td>{Text(Blank(cell))}</td>");
            html.AppendLine("</tr>");
        }
        html.AppendLine("</tbody></table>");
    }

    private static void AppendRow(StringBuilder html, string label, string? value)
        => html.AppendLine($"<tr><th>{Text(label)}</th><td>{Text(Blank(value))}</td></tr>");

    private string ProtocolVersionName(Guid versionId)
    {
        var version = store.ClinicalProtocolVersions.FirstOrDefault(v => v.ClinicalProtocolVersionId == versionId);
        if (version is null) return "-";
        var protocol = store.ClinicalProtocols.FirstOrDefault(p => p.ClinicalProtocolId == version.ClinicalProtocolId);
        return $"{protocol?.ProtocolCode ?? protocol?.Name ?? "PD"} v{version.VersionNo} - {version.Title}";
    }

    private string DepartmentName(Guid? id) => id.HasValue ? store.Departments.FirstOrDefault(d => d.DepartmentId == id.Value)?.Name ?? "-" : "-";
    private string EncounterCode(Guid? id) => id.HasValue ? store.EncounterRefs.FirstOrDefault(e => e.EncounterRefId == id.Value)?.ExternalEncounterId ?? "-" : "-";
    private string ServiceName(Guid id) => store.TechnicalServices.FirstOrDefault(s => s.TechnicalServiceId == id)?.Name ?? "-";
    private static string DisplayPatient(Sql.PatientRef patient) => patient.DisplayName ?? patient.PatientCode ?? patient.ExternalPatientId;
    private static string Lookup(IReadOnlyList<Sql.LookupEntry> entries, string? code) => entries.FirstOrDefault(e => e.Code == code)?.Name ?? code ?? "-";
    private static string Blank(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value;
    private static string Text(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
