using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using TelemedicineLandingPage.Application.Signature;
using TelemedicineLandingPage.Models.Admin;
using ModelsSql = TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin;

public interface IClinicalExportService
{
    string BuildWorkspaceHtmlReport(DateTime generatedAtUtc);
    string BuildPatientDossierHtmlReport(Guid patientRefId, DateTime generatedAtUtc);
}

public sealed class ClinicalExportService(IMedDataStore store, IWebHostEnvironment? environment = null) : IClinicalExportService
{
    private const string SignatureTargetType = "patient_protocol_application";
    private const string PngDataUrlPrefix = "data:image/png;base64,";
    private const string JpegDataUrlPrefix = "data:image/jpeg;base64,";
    private static readonly byte[] PngHeader = [137, 80, 78, 71, 13, 10, 26, 10];

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

    public string BuildPatientDossierHtmlReport(Guid patientRefId, DateTime generatedAtUtc)
    {
        var patient = store.PatientRefs.FirstOrDefault(p => p.PatientRefId == patientRefId)
            ?? throw new InvalidOperationException("Khong tim thay ho so benh nhan.");
        var html = new StringBuilder();

        html.AppendLine("<!doctype html><html lang=\"vi\"><head><meta charset=\"utf-8\">");
        html.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        html.AppendLine("<title>H\u1ed3 s\u01a1 l\u00e2m s\u00e0ng</title>");
        html.AppendLine("<style>");
        html.AppendLine("@page{size:A4;margin:16mm 14mm 18mm}*{box-sizing:border-box}body{font-family:\"Times New Roman\",serif;margin:0;color:#111827;background:#fff;font-size:13px;line-height:1.45}");
        html.AppendLine(".document-header{display:flex;gap:14px;align-items:center;border-bottom:2px solid #0f3b69;padding-bottom:10px}.logo{width:68px;height:68px;object-fit:contain}.logo-fallback{width:68px;height:68px;border:1px solid #98a2b3;display:flex;align-items:center;justify-content:center;font-weight:700}.hospital{font-size:16px;font-weight:700;text-transform:uppercase}.muted{color:#667085}.document-title{text-align:center;margin:18px 0 4px;font-size:22px}.document-meta{text-align:center;margin:0 0 16px}.document-section{margin-top:16px;break-inside:avoid}.document-section h2{font-size:15px;margin:0 0 7px;text-transform:uppercase}h3{font-size:14px;margin:12px 0 6px}table{width:100%;border-collapse:collapse;margin:6px 0 12px}th,td{border:1px solid #98a2b3;padding:6px;text-align:left;vertical-align:top}th{background:#f2f4f7}.empty{color:#98a2b3;font-style:italic}.signature-evidence{border:1px solid #98a2b3;padding:10px;margin-top:10px;break-inside:avoid}.signature-image{display:block;max-width:320px;max-height:120px;margin-top:8px;border-bottom:1px solid #344054;filter:brightness(0) contrast(10);opacity:1}.signature-stamp{width:260px;min-height:96px;margin-top:10px;border:2px solid #0f3b69;border-radius:8px;padding:10px;text-align:center;color:#0f3b69}.signature-stamp-title{font-weight:700;text-transform:uppercase;letter-spacing:.04em}.signature-stamp-name{font-size:20px;font-weight:700;margin:8px 0 4px}.signature-stamp-meta{font-size:12px;color:#344054}.revoked{color:#b42318;font-weight:700}.document-footer{margin-top:22px;border-top:1px solid #98a2b3;padding-top:8px;font-size:12px}");
        html.AppendLine("</style></head><body>");
        html.AppendLine("<header class=\"document-header\">");
        AppendLogo(html);
        html.AppendLine($"<div><div class=\"hospital\">{Text(HospitalName())}</div><div class=\"muted\">QLCM Pro - H\u1ed3 s\u01a1 chuy\u00ean m\u00f4n</div></div>");
        html.AppendLine("</header>");
        html.AppendLine("<h1 class=\"document-title\">H\u1ed2 S\u01a0 L\u00c2M S\u00c0NG</h1>");
        html.AppendLine($"<p class=\"document-meta\">M\u00e3 h\u1ed3 s\u01a1: {Text(Blank(patient.PatientCode))} &bull; Xu\u1ea5t l\u00fac {Text(AdminDateTimeDisplay.DateTime(generatedAtUtc))}</p>");

        html.AppendLine("<section class=\"document-section\"><h2>I. TH\u00d4NG TIN NG\u01af\u1edcI B\u1ec6NH</h2>");
        AppendPatientDetails(html, patient, professionalLabels: true);
        html.AppendLine("</section>");
        html.AppendLine("<section class=\"document-section\"><h2>II. L\u01af\u1ee2T KH\u00c1M</h2>");
        AppendEncounters(html, patientRefId, heading: null, professionalLabels: true);
        html.AppendLine("</section>");
        html.AppendLine("<section class=\"document-section\"><h2>III. PH\u00c1C \u0110\u1ed2 \u0110\u00c3 \u00c1P D\u1ee4NG</h2>");
        AppendProtocolApplications(html, patientRefId, heading: null, includeSignatureEvidence: true, professionalLabels: true);
        html.AppendLine("</section>");
        html.AppendLine("<section class=\"document-section\"><h2>IV. CH\u1ec8 \u0110\u1ecaNH K\u1ef8 THU\u1eacT LI\u00caN QUAN</h2>");
        AppendTechnicalOrders(html, patientRefId, heading: null, professionalLabels: true);
        html.AppendLine("</section>");
        html.AppendLine($"<footer class=\"document-footer\">{Text(HospitalName())} &bull; T\u00e0i li\u1ec7u in t\u1eeb QLCM Pro.</footer>");
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

    private void AppendPatient(StringBuilder html, ModelsSql.PatientRef patient)
    {
        html.AppendLine("<section class=\"patient\">");
        html.AppendLine($"<h2>{Text(DisplayPatient(patient))}</h2>");
        AppendPatientDetails(html, patient);
        AppendEncounters(html, patient.PatientRefId);
        AppendProtocolApplications(html, patient.PatientRefId);
        AppendTechnicalOrders(html, patient.PatientRefId);
        html.AppendLine("</section>");
    }

    private static void AppendPatientDetails(StringBuilder html, ModelsSql.PatientRef patient, bool professionalLabels = false)
    {
        html.AppendLine("<table><tbody>");
        if (professionalLabels) AppendRow(html, "H\u1ecd t\u00ean", DisplayPatient(patient));
        AppendRow(html, professionalLabels ? "M\u00e3 b\u1ec7nh nh\u00e2n" : "Ma benh nhan", patient.PatientCode);
        AppendRow(html, professionalLabels ? "M\u00e3 ngo\u00e0i" : "Ma ngoai", patient.ExternalPatientId);
        AppendRow(html, professionalLabels ? "Ngu\u1ed3n" : "Nguon", patient.SourceSystemCode);
        AppendRow(html, professionalLabels ? "Ng\u00e0y sinh" : "Ngay sinh", patient.BirthDate?.ToString("dd/MM/yyyy"));
        AppendRow(html, professionalLabels ? "Gi\u1edbi t\u00ednh" : "Gioi tinh", Lookup(ModelsSql.MedLookups.Genders, patient.GenderCode));
        AppendRow(html, professionalLabels ? "Ng\u00e0y t\u1ea1o" : "Ngay tao", AdminDateTimeDisplay.DateTime(patient.CreatedAt));
        html.AppendLine("</tbody></table>");
    }

    private void AppendEncounters(StringBuilder html, Guid patientId, string? heading = "Luot kham", bool professionalLabels = false)
    {
        var encounters = store.EncounterRefs
            .Where(e => e.PatientRefId == patientId)
            .OrderByDescending(e => e.StartedAt)
            .ToList();

        if (heading is not null) html.AppendLine($"<h3>{Text(heading)}</h3>");
        var headers = professionalLabels
            ? new[] { "M\u00e3 l\u01b0\u1ee3t kh\u00e1m", "Lo\u1ea1i", "Khoa", "B\u1eaft \u0111\u1ea7u", "K\u1ebft th\u00fac" }
            : new[] { "Ma luot kham", "Loai", "Khoa", "Bat dau", "Ket thuc" };
        AppendTable(html, headers, encounters,
            e => new[]
            {
                e.ExternalEncounterId,
                e.EncounterType,
                DepartmentName(e.DepartmentId),
                AdminDateTimeDisplay.DateTime(e.StartedAt),
                AdminDateTimeDisplay.DateTime(e.EndedAt)
            });
    }

    private void AppendProtocolApplications(
        StringBuilder html,
        Guid patientId,
        string? heading = "Phac do da ap dung",
        bool includeSignatureEvidence = false,
        bool professionalLabels = false)
    {
        var apps = store.PatientProtocolApplications
            .Where(a => a.PatientRefId == patientId)
            .OrderByDescending(a => a.AppliedAt)
            .ToList();

        if (heading is not null) html.AppendLine($"<h3>{Text(heading)}</h3>");
        var headers = professionalLabels
            ? new[] { "Ph\u00e1c \u0111\u1ed3", "L\u01b0\u1ee3t kh\u00e1m", "Ch\u1ea9n \u0111o\u00e1n", "Tr\u1ea1ng th\u00e1i", "Th\u1eddi \u0111i\u1ec3m", "L\u00fd do / ng\u1eef c\u1ea3nh" }
            : new[] { "Phac do", "Luot kham", "Chan doan", "Trang thai", "Thoi diem", "Ly do/ngu canh" };
        AppendTable(html, headers, apps,
            a => new[]
            {
                ProtocolVersionName(a.ClinicalProtocolVersionId),
                EncounterCode(a.EncounterRefId),
                a.DiagnosisCode,
                Lookup(ModelsSql.MedLookups.ProtocolApplicationStatuses, a.ApplicationStatus),
                AdminDateTimeDisplay.DateTime(a.AppliedAt),
                string.IsNullOrWhiteSpace(a.SkippedReason) ? a.DecisionContextJson : a.SkippedReason
            });

        if (includeSignatureEvidence)
        {
            foreach (var app in apps.Where(a => a.ApplicationStatus is "signed" or "revoked" || SignatureFor(a.PatientProtocolApplicationId) is not null))
            {
                AppendSignatureEvidence(html, app);
            }
        }
    }

    private void AppendTechnicalOrders(StringBuilder html, Guid patientId, string? heading = "Chi dinh ky thuat lien quan", bool professionalLabels = false)
    {
        var orders = store.TechnicalOrders
            .Where(o => o.PatientRefId == patientId)
            .OrderByDescending(o => o.OrderedAt)
            .ToList();

        if (heading is not null) html.AppendLine($"<h3>{Text(heading)}</h3>");
        var headers = professionalLabels
            ? new[] { "D\u1ecbch v\u1ee5", "L\u01b0\u1ee3t kh\u00e1m", "Khoa", "Tr\u1ea1ng th\u00e1i", "Ng\u00e0y t\u1ea1o", "Ho\u00e0n t\u1ea5t" }
            : new[] { "Dich vu", "Luot kham", "Khoa", "Trang thai", "Ngay tao", "Hoan tat" };
        AppendTable(html, headers, orders,
            o => new[]
            {
                ServiceName(o.TechnicalServiceId),
                EncounterCode(o.EncounterRefId),
                DepartmentName(o.OrderingDepartmentId),
                Lookup(ModelsSql.MedLookups.OrderStatuses, o.OrderStatus),
                AdminDateTimeDisplay.DateTime(o.OrderedAt),
                AdminDateTimeDisplay.DateTime(o.CompletedAt)
            });
    }

    private void AppendSignatureEvidence(StringBuilder html, ModelsSql.PatientProtocolApplication app)
    {
        var signature = SignatureFor(app.PatientProtocolApplicationId);

        html.AppendLine("<article class=\"signature-evidence\">");
        html.AppendLine($"<h3>X\u00e1c nh\u1eadn ch\u1eef k\u00fd: {Text(ProtocolVersionName(app.ClinicalProtocolVersionId))}</h3>");
        if (app.ApplicationStatus == "revoked")
        {
            html.AppendLine("<p class=\"revoked\">CH\u1eee K\u00dd \u0110\u00c3 THU H\u1ed2I</p>");
        }

        if (signature is null)
        {
            html.AppendLine("<p class=\"empty\">Kh\u00f4ng c\u00f3 b\u1eb1ng ch\u1ee9ng ch\u1eef k\u00fd \u0111\u00e3 l\u01b0u.</p>");
            html.AppendLine("</article>");
            return;
        }

        html.AppendLine("<table><tbody>");
        AppendRow(html, "Ng\u01b0\u1eddi k\u00fd", signature.SignerUsername);
        AppendRow(html, "Th\u1eddi \u0111i\u1ec3m k\u00fd", AdminDateTimeDisplay.DateTime(signature.SignedAt));
        AppendRow(html, "Nh\u00e0 cung c\u1ea5p", signature.ProviderCode);
        html.AppendLine("</tbody></table>");
        if (TryReadSignatureImageDataUrl(signature.MetadataJson, out var imageDataUrl))
        {
            html.AppendLine($"<img class=\"signature-image\" src=\"{Text(imageDataUrl)}\" alt=\"B\u1eb1ng ch\u1ee9ng ch\u1eef k\u00fd\">");
        }
        else
        {
            AppendSignatureStamp(html, signature);
        }
        html.AppendLine("</article>");
    }

    private static void AppendSignatureStamp(StringBuilder html, ModelsSql.SignatureRecord signature)
    {
        html.AppendLine("<div class=\"signature-stamp\" aria-label=\"Chữ ký điện tử\">");
        html.AppendLine("<div class=\"signature-stamp-title\">Đã ký điện tử</div>");
        html.AppendLine($"<div class=\"signature-stamp-name\">{Text(signature.SignerUsername)}</div>");
        html.AppendLine($"<div class=\"signature-stamp-meta\">{Text(AdminDateTimeDisplay.DateTime(signature.SignedAt))}</div>");
        html.AppendLine("<div class=\"signature-stamp-meta\">Xác nhận ký điện tử - hồ sơ QLCM Pro</div>");
        html.AppendLine("</div>");
    }

    private void AppendLogo(StringBuilder html)
    {
        var dataUrl = ReadLogoDataUrl();
        if (dataUrl is null)
        {
            html.AppendLine("<div class=\"logo-fallback\" aria-label=\"Logo bệnh viện\">BV</div>");
            return;
        }

        html.AppendLine($"<img class=\"logo\" src=\"{Text(dataUrl)}\" alt=\"Logo bệnh viện\">");
    }

    private string? ReadLogoDataUrl()
    {
        var logoPath = LogoPathCandidates().FirstOrDefault(File.Exists);
        return logoPath is not null
            ? JpegDataUrlPrefix + Convert.ToBase64String(File.ReadAllBytes(logoPath))
            : null;
    }

    private IEnumerable<string> LogoPathCandidates()
    {
        if (!string.IsNullOrWhiteSpace(environment?.WebRootPath))
        {
            yield return Path.Combine(environment.WebRootPath, "brand", "logo-hos.jpg");
        }

        foreach (var root in WalkUp(AppContext.BaseDirectory).Concat(WalkUp(Directory.GetCurrentDirectory())))
        {
            yield return Path.Combine(root, "wwwroot", "brand", "logo-hos.jpg");
            yield return Path.Combine(root, "src", "telemedicine-landing-page", "wwwroot", "brand", "logo-hos.jpg");
        }
    }

    private static IEnumerable<string> WalkUp(string start)
    {
        var current = new DirectoryInfo(start);
        while (current is not null)
        {
            yield return current.FullName;
            current = current.Parent;
        }
    }

    private ModelsSql.SignatureRecord? SignatureFor(Guid applicationId)
        => store.SignatureRecords.FirstOrDefault(s =>
            s.TargetType == SignatureTargetType &&
            s.TargetId == applicationId);

    private static bool TryReadSignatureImageDataUrl(string? metadataJson, out string imageDataUrl)
    {
        imageDataUrl = string.Empty;
        if (string.IsNullOrWhiteSpace(metadataJson) ||
            metadataJson.Length > SignatureService.MaxMetadataJsonChars)
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(metadataJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !TryGetProperty(document.RootElement, "SignatureImageDataUrl", out var image))
            {
                return false;
            }

            var candidate = image.GetString();
            if (string.IsNullOrWhiteSpace(candidate) ||
                !candidate.StartsWith(PngDataUrlPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            var base64 = candidate[PngDataUrlPrefix.Length..];
            var maxBase64Chars = ((SignatureService.MaxSignatureImageBytes + 2) / 3) * 4;
            if (base64.Length == 0 || base64.Length > maxBase64Chars)
                return false;

            var bytes = Convert.FromBase64String(base64);
            if (bytes.Length > SignatureService.MaxSignatureImageBytes ||
                bytes.Length < PngHeader.Length ||
                !bytes.AsSpan(0, PngHeader.Length).SequenceEqual(PngHeader))
            {
                return false;
            }

            imageDataUrl = candidate;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
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
    private string HospitalName() => store.Departments.FirstOrDefault(d => d.ParentDepartmentId is null)?.Name ?? "Benh vien";
    private string EncounterCode(Guid? id) => id.HasValue ? store.EncounterRefs.FirstOrDefault(e => e.EncounterRefId == id.Value)?.ExternalEncounterId ?? "-" : "-";
    private string ServiceName(Guid id)
    {
        var service = store.TechnicalServices.FirstOrDefault(s => s.TechnicalServiceId == id);
        return service is null ? "-" : $"{service.ServiceCode} - {service.Name}";
    }
    private static string DisplayPatient(ModelsSql.PatientRef patient) => patient.DisplayName ?? patient.PatientCode ?? patient.ExternalPatientId;
    private static string Lookup(IReadOnlyList<ModelsSql.LookupEntry> entries, string? code) => entries.FirstOrDefault(e => e.Code == code)?.Name ?? code ?? "-";
    private static string Blank(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value;
    private static string Text(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
