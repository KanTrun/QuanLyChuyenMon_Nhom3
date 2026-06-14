using System.Net;
using System.Text;
using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin;

public sealed class ProcedureDocumentExportService : IProcedureDocumentExportService
{
    private const string HospitalName = "BỆNH VIỆN UNG BƯỚU";
    private const int SectionPageCharacterLimit = 2800;
    private const int FlowStepsPerPage = 4;

    private readonly ProcedureDocumentSnapshotService _snapshots;
    private readonly IWebHostEnvironment? _environment;

    public ProcedureDocumentExportService(
        ProcedureDocumentSnapshotService snapshots,
        IWebHostEnvironment? environment = null)
    {
        _snapshots = snapshots;
        _environment = environment;
    }

    public string BuildProcedureDocumentHtml(Guid procedureVersionId, DateTime generatedAt)
    {
        var snapshot = _snapshots.GetSnapshot(procedureVersionId);
        var hash = _snapshots.ComputeContentHash(procedureVersionId);
        var readiness = _snapshots.CheckReadiness(procedureVersionId, requireSignoffs: true);
        var pages = BuildPages(snapshot, hash, readiness, generatedAt);
        var versionLabel = snapshot.Version.VersionLabel ?? $"v{snapshot.Version.VersionNo:00}";
        var renderedPages = string.Join("", pages.Select((page, index) =>
            RenderPage(snapshot, page, index + 1, pages.Count, versionLabel)));

        return $$"""
<!doctype html>
<html lang="vi">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>{{H(snapshot.Procedure.ProcedureCode)}} - {{H(snapshot.Version.Title)}}</title>
<style>{{PrintCss}}</style>
</head>
<body>
<div class="print-toolbar no-print">
  <button type="button" onclick="window.print()">In / Lưu PDF</button>
  <span>{{pages.Count}} trang A4 • {{H(snapshot.Procedure.ProcedureCode)}} • {{H(versionLabel)}}</span>
</div>
{{renderedPages}}
</body>
</html>
""";
    }

    private List<string> BuildPages(
        ProcedureDocumentSnapshot s,
        string hash,
        ProcedureDocumentReadiness readiness,
        DateTime generatedAt)
    {
        var pages = new List<string>
        {
            CoverPage(s, hash, readiness),
            ControlPage(s)
        };

        foreach (var section in s.Sections)
        {
            var chunks = SplitContent(section.ContentText, SectionPageCharacterLimit);
            for (var i = 0; i < chunks.Count; i++)
            {
                var continuation = chunks.Count > 1 ? $" <span class=\"continuation\">({i + 1}/{chunks.Count})</span>" : "";
                pages.Add($"<h1 class=\"section-title\">{H(section.SectionNumber)}. {H(section.Title)}{continuation}</h1><div class=\"section-body\">{H(chunks[i])}</div>");
            }
        }

        var flowGroups = s.Steps.Chunk(FlowStepsPerPage).ToList();
        for (var i = 0; i < flowGroups.Count; i++)
        {
            pages.Add(FlowPage(flowGroups[i], i + 1, flowGroups.Count));
        }

        pages.Add(TraceabilityPage(s, hash, generatedAt));
        return pages;
    }

    private string CoverPage(ProcedureDocumentSnapshot s, string hash, ProcedureDocumentReadiness readiness)
    {
        var logo = LogoHtml("cover-logo");
        var warning = readiness.IsReady
            ? "<div class=\"ready-banner\">Hồ sơ đã đủ điều kiện kiểm soát nội bộ</div>"
            : $"<div class=\"warning-banner\"><strong>Chưa đủ điều kiện ban hành:</strong> {H(string.Join("; ", readiness.MissingItems))}</div>";

        return $$"""
<div class="cover-brand">{{logo}}<div><div class="hospital-name">{{HospitalName}}</div><div class="hospital-unit">HỆ THỐNG QUẢN LÝ CHUYÊN MÔN</div></div></div>
<div class="document-category">QUY TRÌNH CHUYÊN MÔN</div>
<h1 class="cover-title">{{H(s.Version.Title)}}</h1>
<div class="document-code">Mã số: <strong>{{H(s.Procedure.ProcedureCode)}}</strong> • Phiên bản: <strong>{{H(s.Version.VersionLabel ?? $"v{s.Version.VersionNo:00}")}}</strong></div>
<table class="meta-table">
<tr><th>Khoa/phòng chủ trì</th><td>{{H(s.DepartmentName)}}</td><th>Lần ban hành</th><td>{{H(s.Version.IssueNumber)}}</td></tr>
<tr><th>Ngày ban hành</th><td>{{D(s.Version.IssueDate)}}</td><th>Trạng thái</th><td>{{H(StatusLabel(s.Version.StatusCode))}}</td></tr>
</table>
{{warning}}
<h2 class="block-title">Xác nhận và phê duyệt nội bộ</h2>
<div class="signature-grid">
{{SignCard(s, "writer", "Người viết", hash)}}
{{SignCard(s, "checker", "Người kiểm tra", hash)}}
{{SignCard(s, "approver", "Người phê duyệt", hash)}}
</div>
""";
    }

    private static string ControlPage(ProcedureDocumentSnapshot s)
    {
        var recipients = s.Recipients.Count == 0
            ? "<tr><td colspan=\"3\" class=\"empty\">Chưa khai báo nơi nhận</td></tr>"
            : string.Join("", s.Recipients.Select(r => $"<tr><td>{r.DisplayOrder}</td><td>{H(r.RecipientName)}</td><td>{(r.IsMarked ? "Bản kiểm soát" : "Bản tham khảo")}</td></tr>"));
        var revisions = s.Revisions.Count == 0
            ? "<tr><td colspan=\"5\" class=\"empty\">Chưa có lịch sử sửa đổi</td></tr>"
            : string.Join("", s.Revisions.Select(r => $"<tr><td>{r.DisplayOrder}</td><td>{D(r.RevisionDate)}</td><td>{H(r.PageRef)}</td><td>{H(r.SectionRef)}</td><td>{H(r.Summary)}</td></tr>"));

        return $$"""
<h1 class="section-title">KIỂM SOÁT TÀI LIỆU</h1>
<table class="meta-table control-meta">
<tr><th>PDF nguồn</th><td colspan="3">{{H(s.Version.SourcePdfFileName)}}</td></tr>
<tr><th>SHA-256 nguồn</th><td colspan="3" class="hash">{{H(s.Version.SourcePdfChecksumSha256)}}</td></tr>
<tr><th>Lý do thay đổi</th><td colspan="3">{{H(s.Version.ChangeReason)}}</td></tr>
</table>
<h2 class="block-title">Nơi nhận và phân phối</h2>
<table><thead><tr><th>STT</th><th>Đơn vị nhận</th><th>Loại bản</th></tr></thead><tbody>{{recipients}}</tbody></table>
<h2 class="block-title">Theo dõi sửa đổi</h2>
<table><thead><tr><th>Lần</th><th>Ngày</th><th>Trang</th><th>Mục</th><th>Nội dung thay đổi</th></tr></thead><tbody>{{revisions}}</tbody></table>
""";
    }

    private static string FlowPage(IReadOnlyList<ProcedureStep> steps, int page, int total)
    {
        var nodes = string.Join("", steps.Select((step, index) => $$"""
<div class="flow-row">
  <div class="flow-track"><div class="flow-symbol shape-{{Shape(step.FlowShapeCode)}}"><span>{{H(step.StepNo)}}</span></div>{{(index < steps.Count - 1 ? "<div class=\"flow-arrow\">↓</div>" : "")}}</div>
  <div class="flow-detail">
    <div class="flow-heading"><strong>{{H(step.StepCode)}} • {{H(step.Name)}}</strong><span>{{H(step.StandardDurationMinutes)}} phút</span></div>
    <dl><dt>Chịu trách nhiệm</dt><dd>{{H(step.ResponsibilityText)}}</dd><dt>Diễn giải</dt><dd>{{H(step.Description)}}</dd><dt>Biểu mẫu/tài liệu</dt><dd>{{H(step.FormReferenceText)}}</dd><dt>Mục liên quan</dt><dd>{{H(step.DetailSectionNumber)}}</dd></dl>
  </div>
</div>
"""));
        return $"<h1 class=\"section-title\">LƯU ĐỒ QUY TRÌNH <span class=\"continuation\">({page}/{total})</span></h1><div class=\"flow-list\">{nodes}</div>";
    }

    private static string TraceabilityPage(ProcedureDocumentSnapshot s, string hash, DateTime generatedAt)
    {
        var attachments = s.Attachments.Count == 0
            ? "<tr><td colspan=\"5\" class=\"empty\">Không có tệp đính kèm</td></tr>"
            : string.Join("", s.Attachments.Select((a, index) => $"<tr><td>{index + 1}</td><td>{H(a.FileName)}</td><td>{H(AttachmentLabel(a.AttachmentType))}</td><td>{FileSize(a.FileSizeBytes)}</td><td class=\"hash\">{H(a.ChecksumSha256)}</td></tr>"));
        var signoffs = string.Join("", s.Signoffs.Select((sign, index) => $"<tr><td>{index + 1}</td><td>{H(RoleLabel(sign.SignoffRole))}</td><td>{H(sign.SignerFullName ?? sign.SignerUsername)}</td><td>{D(sign.SignedAt)}</td><td class=\"hash\">{H(sign.ContentHashSha256)}</td></tr>"));
        if (string.IsNullOrEmpty(signoffs)) signoffs = "<tr><td colspan=\"5\" class=\"empty\">Chưa có xác nhận nội bộ</td></tr>";

        return $$"""
<h1 class="section-title">HỒ SƠ, TỆP VÀ DẤU VẾT KIỂM SOÁT</h1>
<h2 class="block-title">Tệp gắn kèm</h2>
<table><thead><tr><th>STT</th><th>Tên tệp</th><th>Loại</th><th>Dung lượng</th><th>SHA-256</th></tr></thead><tbody>{{attachments}}</tbody></table>
<h2 class="block-title">Nhật ký chữ ký nội bộ</h2>
<table><thead><tr><th>STT</th><th>Vai trò</th><th>Người xác nhận</th><th>Thời điểm</th><th>Hash nội dung</th></tr></thead><tbody>{{signoffs}}</tbody></table>
<div class="trace-box"><strong>Hash nội dung hiện tại</strong><div class="hash">{{H(hash)}}</div><div>Thời điểm tạo bản in: {{D(generatedAt)}}</div></div>
""";
    }

    private string RenderPage(ProcedureDocumentSnapshot s, string content, int page, int total, string versionLabel)
        => $$"""
<section class="page">
  <header class="page-header"><div class="mini-brand">{{LogoHtml("mini-logo")}}<div><strong>{{HospitalName}}</strong><span>QUẢN LÝ CHUYÊN MÔN</span></div></div><div class="page-document"><strong>{{H(s.Procedure.ProcedureCode)}}</strong><span>{{H(versionLabel)}}</span></div></header>
  <main>{{content}}</main>
  <footer class="page-footer"><span>{{H(s.Version.Title)}}</span><span>Trang {{page}} / {{total}}</span></footer>
</section>
""";

    private string LogoHtml(string cssClass)
    {
        var dataUrl = ReadLogoDataUrl();
        return dataUrl is null
            ? $"<div class=\"{cssClass} logo-fallback\">BV</div>"
            : $"<img class=\"{cssClass}\" src=\"{H(dataUrl)}\" alt=\"Logo Bệnh viện Ung Bướu\">";
    }

    private string? ReadLogoDataUrl()
    {
        var candidates = new[]
        {
            _environment?.WebRootPath is { Length: > 0 } root ? Path.Combine(root, "brand", "logo-hos.jpg") : null,
            Path.Combine(AppContext.BaseDirectory, "wwwroot", "brand", "logo-hos.jpg")
        };
        var path = candidates.FirstOrDefault(candidate => candidate is not null && File.Exists(candidate));
        return path is null ? null : "data:image/jpeg;base64," + Convert.ToBase64String(File.ReadAllBytes(path));
    }

    private static string SignCard(ProcedureDocumentSnapshot s, string role, string label, string currentHash)
    {
        var sign = s.Signoffs.Where(x => x.SignoffRole == role && x.ContentHashSha256.Equals(currentHash, StringComparison.OrdinalIgnoreCase)).OrderByDescending(x => x.SignedAt).FirstOrDefault();
        if (sign is null) return $"<div class=\"signature-card\"><h3>{label}</h3><div class=\"signature-space\"></div><div class=\"empty\">Chưa ký</div></div>";
        var image = sign.SignatureImageDataUrl?.StartsWith("data:image/png;base64,", StringComparison.OrdinalIgnoreCase) == true
            ? $"<img class=\"signature-image\" src=\"{H(sign.SignatureImageDataUrl)}\" alt=\"Chữ ký {H(label)}\">"
            : "<div class=\"signature-space signed-mark\">ĐÃ XÁC NHẬN</div>";
        return $"<div class=\"signature-card\"><h3>{label}</h3>{image}<strong>{H(sign.SignerFullName ?? sign.SignerUsername)}</strong><span>{D(sign.SignedAt)}</span><span class=\"hash short-hash\">{H(sign.ContentHashSha256[..Math.Min(16, sign.ContentHashSha256.Length)])}…</span></div>";
    }

    private static IReadOnlyList<string> SplitContent(string? value, int limit)
    {
        var text = string.IsNullOrWhiteSpace(value) ? "Chưa có nội dung." : value.Trim();
        var chunks = new List<string>();
        for (var offset = 0; offset < text.Length; offset += limit)
        {
            var length = Math.Min(limit, text.Length - offset);
            if (offset + length < text.Length)
            {
                var breakAt = text.LastIndexOfAny(['\n', '.', ';'], offset + length - 1, length);
                if (breakAt > offset + limit / 2) length = breakAt - offset + 1;
            }
            chunks.Add(text.Substring(offset, length).Trim());
            offset -= limit - length;
        }
        return chunks;
    }

    private static string H(object? value) => WebUtility.HtmlEncode(value?.ToString() ?? "");
    private static string D(DateTime? value) => value?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "";
    private static string Shape(string? shape) => shape is "terminator" or "decision" or "data" or "document" ? shape : "process";
    private static string StatusLabel(string status) => status switch { "draft" => "Bản nháp", "pending_approval" => "Chờ phê duyệt", "active" => "Đang hiệu lực", "superseded" => "Đã được thay thế", "archived" => "Lưu trữ", "rejected" => "Bị từ chối", _ => status };
    private static string RoleLabel(string role) => role switch { "writer" => "Người viết", "checker" => "Người kiểm tra", "approver" => "Người phê duyệt", _ => role };
    private static string AttachmentLabel(string type) => type == "source_pdf" ? "PDF nguồn" : "Tệp đính kèm";
    private static string FileSize(long? bytes) => bytes.HasValue ? $"{bytes.Value / 1024d / 1024d:0.##} MB" : "";

    private const string PrintCss = """
@page{size:A4;margin:0}*{box-sizing:border-box}html,body{margin:0;padding:0}body{font-family:"Times New Roman",serif;color:#111;background:#d7dce2;font-size:12.5pt;line-height:1.42}.print-toolbar{position:sticky;top:0;z-index:20;display:flex;align-items:center;justify-content:center;gap:16px;padding:10px;background:#10263d;color:#fff;font-family:Arial,sans-serif}.print-toolbar button{border:0;border-radius:4px;background:#69be45;color:#10263d;font-weight:700;padding:9px 18px;cursor:pointer}.page{width:210mm;min-height:297mm;margin:12px auto;background:#fff;padding:10mm 14mm 12mm;display:flex;flex-direction:column;box-shadow:0 5px 22px #0002;break-after:page;page-break-after:always}.page:last-child{break-after:auto;page-break-after:auto}.page-header{height:19mm;border-bottom:1.5px solid #1f5ea8;display:flex;align-items:center;justify-content:space-between;padding-bottom:3mm}.mini-brand{display:flex;align-items:center;gap:3mm}.mini-brand div{display:flex;flex-direction:column}.mini-brand strong{font-size:10pt;color:#1f5ea8}.mini-brand span{font:7.5pt Arial,sans-serif;color:#555}.mini-logo{width:13mm;height:13mm;object-fit:contain}.page-document{text-align:right;display:flex;flex-direction:column}.page-document strong{color:#1f5ea8}.page-document span{font-size:10pt}.page main{flex:1;padding-top:7mm}.page-footer{border-top:1px solid #777;padding-top:2.5mm;margin-top:6mm;display:flex;justify-content:space-between;font-size:9pt;color:#555}.cover-brand{display:flex;align-items:center;justify-content:center;gap:8mm;margin-top:7mm;text-align:left}.cover-logo{width:32mm;height:32mm;object-fit:contain}.hospital-name{font-weight:700;color:#1f5ea8;font-size:18pt}.hospital-unit{font:9pt Arial,sans-serif;letter-spacing:.08em;color:#555}.document-category{text-align:center;margin-top:14mm;color:#1f5ea8;font-weight:700;letter-spacing:.14em}.cover-title{text-align:center;text-transform:uppercase;font-size:24pt;line-height:1.25;margin:8mm 8mm}.document-code{text-align:center;margin-bottom:10mm}.section-title{font-size:17pt;color:#1f5ea8;border-bottom:2px solid #69be45;padding-bottom:3mm;margin:0 0 8mm}.continuation{font-size:10pt;color:#555;font-weight:400}.block-title{font-size:13pt;color:#1f5ea8;margin:7mm 0 3mm}.section-body{white-space:pre-wrap;text-align:justify;font-size:13pt;line-height:1.6}.meta-table,table{width:100%;border-collapse:collapse;margin:4mm 0}th,td{border:1px solid #555;padding:2.5mm 3mm;vertical-align:top}th{background:#eaf1f8;text-align:left;color:#163f6d}.meta-table th{width:20%}.warning-banner,.ready-banner{padding:3mm 4mm;margin:5mm 0;border-left:4px solid}.warning-banner{background:#fff4e5;border-color:#b45309}.ready-banner{background:#edf8e8;border-color:#4b9b2f}.signature-grid{display:grid;grid-template-columns:repeat(3,1fr);gap:3mm}.signature-card{border:1px solid #555;min-height:52mm;padding:3mm;text-align:center;display:flex;flex-direction:column;align-items:center}.signature-card h3{margin:0 0 2mm;color:#163f6d}.signature-space{height:24mm;width:100%;display:flex;align-items:center;justify-content:center}.signed-mark{font:700 9pt Arial,sans-serif;color:#2f7f22;border:1px solid #69be45;margin-bottom:2mm}.signature-image{width:100%;height:24mm;object-fit:contain;filter:brightness(0) contrast(10)}.signature-card span{font-size:9pt}.hash{font:8pt Consolas,monospace;word-break:break-all}.short-hash{margin-top:1mm}.empty{color:#777;font-style:italic;text-align:center}.flow-list{display:flex;flex-direction:column;gap:4mm}.flow-row{display:grid;grid-template-columns:38mm 1fr;gap:7mm;min-height:46mm}.flow-track{display:flex;flex-direction:column;align-items:center}.flow-symbol{width:29mm;height:22mm;border:1.5px solid #163f6d;display:flex;align-items:center;justify-content:center;background:#eef5fb;color:#163f6d;font-weight:700}.shape-terminator{border-radius:999px}.shape-process{border-radius:2px}.shape-decision{width:22mm;height:22mm;transform:rotate(45deg);margin:3mm}.shape-decision span{transform:rotate(-45deg)}.shape-data{transform:skew(-13deg)}.shape-data span{transform:skew(13deg)}.shape-document{border-radius:2px 2px 9mm 9mm}.flow-arrow{font-size:19pt;color:#69be45;line-height:1}.flow-detail{border-left:4px solid #69be45;background:#f6f8fa;padding:3mm 4mm}.flow-heading{display:flex;justify-content:space-between;color:#163f6d}.flow-detail dl{display:grid;grid-template-columns:32mm 1fr;margin:2mm 0 0;font-size:10.5pt}.flow-detail dt{font-weight:700}.flow-detail dd{margin:0 0 1mm}.trace-box{border:1.5px solid #1f5ea8;background:#f5f9fd;padding:4mm;margin-top:8mm}.logo-fallback{border:1px solid #1f5ea8;display:flex;align-items:center;justify-content:center;color:#1f5ea8;font-weight:700}@media print{body{background:#fff}.no-print{display:none!important}.page{margin:0;box-shadow:none}}
""";
}
