using System.Net;

namespace TelemedicineLandingPage.Services.Admin;

public sealed class ProcedureDocumentExportService : IProcedureDocumentExportService
{
    private const string PrintCss = """
@page{size:A4;margin:18mm 16mm 20mm}body{font-family:"Times New Roman",serif;color:#111;line-height:1.45}.page{break-after:page}.header,.footer{font-size:12px;color:#444}.cover-title{text-align:center;text-transform:uppercase;font-weight:700;font-size:22px;margin:36px 0 18px}.meta,.signatures,.revisions,.recipients,.attachments{width:100%;border-collapse:collapse;margin:12px 0}.meta td,.signatures th,.signatures td,.revisions th,.revisions td,.recipients td,.attachments th,.attachments td{border:1px solid #333;padding:6px;vertical-align:top}.roman{font-weight:700;text-transform:uppercase;margin-top:18px}.flow{display:flex;flex-direction:column;gap:10px;align-items:center;margin:18px 0}.flow-node{min-width:260px;max-width:420px;border:1.5px solid #111;padding:8px 12px;text-align:center;background:white}.shape-terminator{border-radius:999px}.shape-process{border-radius:3px}.shape-data{transform:skew(-12deg)}.shape-data span{display:block;transform:skew(12deg)}.shape-decision{width:180px;height:100px;display:flex;align-items:center;justify-content:center;transform:rotate(45deg);min-width:0}.shape-decision span{transform:rotate(-45deg);display:block}.shape-document{border-bottom-left-radius:18px;border-bottom-right-radius:18px}.arrow{font-size:18px}.muted{color:#555}.warning{border:1px solid #b45309;background:#fff7ed;padding:8px;margin:12px 0}.hash{font-family:Consolas,monospace;font-size:11px;word-break:break-all}.section-body{white-space:pre-wrap}h2{font-size:15px}@media print{.no-print{display:none}.page{min-height:245mm}}
""";

    private readonly ProcedureDocumentSnapshotService _snapshots;

    public ProcedureDocumentExportService(ProcedureDocumentSnapshotService snapshots)
    {
        _snapshots = snapshots;
    }

    public string BuildProcedureDocumentHtml(Guid procedureVersionId, DateTime generatedAt)
    {
        var s = _snapshots.GetSnapshot(procedureVersionId);
        var hash = _snapshots.ComputeContentHash(procedureVersionId);
        var readiness = _snapshots.CheckReadiness(procedureVersionId, requireSignoffs: true);
        var versionLabel = H(s.Version.VersionLabel ?? $"v{s.Version.VersionNo}");
        var signCells = SignCell(s, "writer", hash) + SignCell(s, "checker", hash) + SignCell(s, "approver", hash);
        var recipientRows = s.Recipients.Count == 0
            ? "<tr><td>Khong co</td><td></td></tr>"
            : string.Join("", s.Recipients.Select(r => $"<tr><td>{H(r.RecipientName)}</td><td>{(r.IsMarked ? "x" : "")}</td></tr>"));
        var revisionRows = string.Join("", s.Revisions.Select(r => $"<tr><td>{r.DisplayOrder}</td><td>{D(r.RevisionDate)}</td><td>{H($"{r.PageRef} {r.SectionRef}".Trim())}</td><td>{H(r.Summary)}</td></tr>"));
        var sectionHtml = string.Join("", s.Sections.Select(sec => $"<h2 class=\"roman\">{H(sec.SectionNumber)}. {H(sec.Title)}</h2><div class=\"section-body\">{H(sec.ContentText)}</div>"));
        var attachmentRows = string.Join("", s.Attachments.Select(a => $"<tr><td>{H(a.FileName)}</td><td>{H(a.AttachmentType)}</td><td>{H(a.FileUri)}</td><td class=\"hash\">{H(a.ChecksumSha256)}</td></tr>"));
        return $"""
<!doctype html>
<html lang="vi">
<head>
<meta charset="utf-8">
<title>{H(s.Procedure.ProcedureCode)} - {H(s.Version.Title)}</title>
<style>
{PrintCss}
</style>
</head>
<body>
<section class="page">
<div class="header">{H(s.Procedure.ProcedureCode)} | {versionLabel}</div>
<div class="cover-title">{H(s.Version.Title)}</div>
<table class="meta">
<tr><td>Ma quy trinh</td><td>{H(s.Procedure.ProcedureCode)}</td><td>Lan ban hanh</td><td>{H(s.Version.IssueNumber?.ToString() ?? "")}</td></tr>
<tr><td>Ngay ban hanh</td><td>{D(s.Version.IssueDate)}</td><td>Khoa/Phong</td><td>{H(s.DepartmentName)}</td></tr>
<tr><td>PDF nguon</td><td colspan="3">{H(s.Version.SourcePdfFileName)}<br><span class="hash">{H(s.Version.SourcePdfChecksumSha256)}</span></td></tr>
</table>
{Warning(readiness.IsReady, readiness.MissingItems)}
<table class="signatures">
<thead><tr><th>Nguoi viet</th><th>Nguoi kiem tra</th><th>Nguoi phe duyet</th></tr></thead>
<tbody><tr>{signCells}</tr></tbody>
</table>
<h2>Noi nhan</h2>
<table class="recipients">{recipientRows}</table>
<h2>Theo doi sua doi</h2>
<table class="revisions"><thead><tr><th>Lan</th><th>Ngay</th><th>Trang/Muc</th><th>Noi dung</th></tr></thead><tbody>{revisionRows}</tbody></table>
</section>
<section class="page">
{sectionHtml}
<h2 class="roman">Luu do quy trinh</h2>
<div class="flow">{Flow(s.Steps)}</div>
<h2 class="roman">Tep dinh kem</h2>
<table class="attachments"><thead><tr><th>Ten tep</th><th>Loai</th><th>URI</th><th>Checksum</th></tr></thead><tbody>{attachmentRows}</tbody></table>
<div class="footer">Generated {D(generatedAt)} | Content hash <span class="hash">{H(hash)}</span></div>
</section>
</body>
</html>
""";
    }

    private static string H(object? value) => WebUtility.HtmlEncode(value?.ToString() ?? "");
    private static string D(DateTime? value) => value.HasValue ? value.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : "";

    private static string Warning(bool ready, IReadOnlyList<string> missing)
        => ready ? "" : $"<div class=\"warning\"><strong>Chua du dieu kien ban hanh:</strong> {H(string.Join(", ", missing))}</div>";

    private static string SignCell(ProcedureDocumentSnapshot s, string role, string currentHash)
    {
        var sign = s.Signoffs
            .Where(x => x.SignoffRole == role && string.Equals(x.ContentHashSha256, currentHash, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.SignedAt)
            .FirstOrDefault();
        return sign is null
            ? "<td><br><br><span class=\"muted\">Chua ky</span></td>"
            : $"<td><strong>{H(sign.SignerFullName ?? sign.SignerUsername)}</strong><br>{D(sign.SignedAt)}<br><span class=\"hash\">{H(sign.ContentHashSha256)}</span></td>";
    }

    private static string Flow(IReadOnlyList<Models.Admin.Sql.ProcedureStep> steps)
        => string.Join("", steps.Select((step, i) =>
            $"<div class=\"flow-node shape-{Shape(step.FlowShapeCode)}\"><span>{H(step.StepNo)}. {H(step.Name)}<br><small>{H(step.ResponsibilityText)}</small></span></div>{(i == steps.Count - 1 ? "" : "<div class=\"arrow\">↓</div>")}"));

    private static string Shape(string? shape)
        => shape is "terminator" or "decision" or "data" or "document" ? shape : "process";

}
