using System.Net;
using System.Text;
using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin;

public sealed class ProcedureDocumentExportService : IProcedureDocumentExportService
{
    private const string HospitalName = "BỆNH VIỆN UNG BƯỚU";
    private const int SectionPageCharacterLimit = 1600;
    private const int SectionPageUnitLimit = 44;
    private const int FlowDescriptionCharacterLimit = 420;
    private const int FlowPageUnitLimit = 48;

    private readonly ProcedureDocumentSnapshotService _snapshots;
    private readonly IProcedureAttachmentAccessTokenService _accessTokens;
    private readonly IWebHostEnvironment? _environment;
    private string? _publicBaseUrl;
    private DateTime _generatedAtUtc;
    private IReadOnlyDictionary<Guid, ProcedureAttachment> _attachmentIndex = new Dictionary<Guid, ProcedureAttachment>();

    public ProcedureDocumentExportService(
        ProcedureDocumentSnapshotService snapshots,
        IProcedureAttachmentAccessTokenService accessTokens,
        IWebHostEnvironment? environment = null)
    {
        _snapshots = snapshots;
        _accessTokens = accessTokens;
        _environment = environment;
    }

    public string BuildProcedureDocumentHtml(Guid procedureVersionId, DateTime generatedAt, string? publicBaseUrl = null)
    {
        var snapshot = _snapshots.GetSnapshot(procedureVersionId);
        _publicBaseUrl = string.IsNullOrWhiteSpace(publicBaseUrl) ? null : publicBaseUrl.TrimEnd('/');
        _generatedAtUtc = generatedAt.ToUniversalTime();
        _attachmentIndex = snapshot.Attachments.ToDictionary(item => item.ProcedureAttachmentId);
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

    private const int CoverPageUnitLimit = 58;
    private const int ControlPageUnitLimit = 52;

    private List<string> BuildPages(
        ProcedureDocumentSnapshot s,
        string hash,
        ProcedureDocumentReadiness readiness,
        DateTime generatedAt)
    {
        var pages = new List<string>();
        pages.AddRange(BuildCoverPages(s, hash, readiness));
        pages.AddRange(BuildControlPages(s, hash));
        pages.AddRange(BuildSectionPages(s.Sections));

        var flowGroups = BuildFlowGroups(s.Steps);
        for (var i = 0; i < flowGroups.Count; i++)
        {
            pages.Add(FlowPage(flowGroups[i], i + 1, flowGroups.Count));
        }

        pages.AddRange(BuildTraceabilityPages(s, hash, generatedAt));
        return pages;
    }

    private IReadOnlyList<string> BuildCoverPages(
        ProcedureDocumentSnapshot snapshot,
        string hash,
        ProcedureDocumentReadiness readiness)
    {
        var cover = CoverPage(snapshot, readiness);
        var signatures = SignatureBlock(snapshot, hash, standalone: false);
        if (EstimateCoverUnits(snapshot, readiness) <= CoverPageUnitLimit)
            return [$"{cover}{signatures}"];

        return [cover, SignatureApprovalPage(snapshot, hash)];
    }

    private IReadOnlyList<string> BuildControlPages(ProcedureDocumentSnapshot snapshot, string currentHash)
    {
        var recipients = snapshot.Recipients.ToList();
        var revisions = snapshot.Revisions.ToList();
        if (EstimateControlUnits(recipients.Count, revisions.Count) <= ControlPageUnitLimit)
            return [ControlPage(snapshot, currentHash, recipients, revisions)];

        const int firstRecipientRows = 4;
        const int firstRevisionRows = 3;
        var pages = new List<string>
        {
            ControlPage(
                snapshot,
                currentHash,
                recipients.Take(firstRecipientRows),
                revisions.Take(firstRevisionRows))
        };

        var remainingRecipients = recipients.Skip(firstRecipientRows).ToList();
        if (remainingRecipients.Count > 0)
        {
            var recipientGroups = remainingRecipients.Chunk(12).ToList();
            for (var index = 0; index < recipientGroups.Count; index++)
                pages.Add(RecipientContinuationPage(recipientGroups[index], index + 1, recipientGroups.Count));
        }

        var remainingRevisions = revisions.Skip(firstRevisionRows).ToList();
        if (remainingRevisions.Count > 0)
        {
            var revisionGroups = remainingRevisions.Chunk(8).ToList();
            for (var index = 0; index < revisionGroups.Count; index++)
                pages.Add(RevisionContinuationPage(revisionGroups[index], index + 1, revisionGroups.Count));
        }

        return pages;
    }

    private const int ControlSummaryUnits = 24;

    private static int EstimateCoverUnits(ProcedureDocumentSnapshot snapshot, ProcedureDocumentReadiness readiness)
    {
        var titleUnits = Math.Clamp((snapshot.Procedure.Name?.Length ?? 0) / 28, 2, 6);
        var warningText = readiness.IsReady ? string.Empty : string.Join("; ", readiness.MissingItems);
        var warningUnits = readiness.IsReady
            ? 2
            : Math.Clamp((warningText.Length / 48) + 2, 3, 8);
        var signatureSlots = Math.Max(1, snapshot.Version.RequiredWriterSignatures) + 2;
        var signatureRows = (int)Math.Ceiling(signatureSlots / 2.0);
        return 26 + titleUnits + warningUnits + 4 + (signatureRows * 11);
    }

    private static int EstimateControlUnits(int recipientCount, int revisionCount)
        => ControlSummaryUnits
           + (int)Math.Ceiling(recipientCount * 1.75)
           + (int)Math.Ceiling(revisionCount * 2.25);

    private IReadOnlyList<string> BuildTraceabilityPages(
        ProcedureDocumentSnapshot snapshot,
        string currentHash,
        DateTime generatedAt)
    {
        const int firstAttachmentRows = 5;
        const int firstSignoffRows = 4;
        var pages = new List<string>
        {
            TraceabilityPage(
                snapshot,
                snapshot.Attachments.Take(firstAttachmentRows),
                snapshot.Signoffs.Take(firstSignoffRows),
                currentHash,
                generatedAt)
        };

        var attachmentGroups = snapshot.Attachments.Skip(firstAttachmentRows).Chunk(9).ToList();
        for (var index = 0; index < attachmentGroups.Count; index++)
            pages.Add(AttachmentContinuationPage(attachmentGroups[index], index + 1, attachmentGroups.Count));

        var signoffGroups = snapshot.Signoffs.Skip(firstSignoffRows).Chunk(7).ToList();
        for (var index = 0; index < signoffGroups.Count; index++)
            pages.Add(SignoffContinuationPage(snapshot, signoffGroups[index], currentHash, index + 1, signoffGroups.Count));

        return pages;
    }

    private static IReadOnlyList<string> BuildSectionPages(IReadOnlyList<ProcedureDocumentSection> sections)
    {
        var pages = new List<string>();
        var currentBlocks = new List<string>();
        var currentUnits = 0;

        foreach (var section in sections)
        {
            var chunks = SplitSectionContent(PrintableText(section.ContentText));
            for (var index = 0; index < chunks.Count; index++)
            {
                var chunk = chunks[index];
                var units = SectionUnits(chunk);
                if (currentBlocks.Count > 0 && currentUnits + units > SectionPageUnitLimit)
                {
                    pages.Add(RenderSectionPage(currentBlocks));
                    currentBlocks.Clear();
                    currentUnits = 0;
                }

                var continuation = chunks.Count > 1
                    ? $" <span class=\"continuation\">({index + 1}/{chunks.Count})</span>"
                    : "";
                currentBlocks.Add($"""
<article class="procedure-section">
  <h1 class="section-title">{H(section.SectionNumber)}. {H(section.Title)}{continuation}</h1>
  <div class="section-body">{H(chunk)}</div>
</article>
""");
                currentUnits += units;
            }
        }

        if (currentBlocks.Count > 0)
            pages.Add(RenderSectionPage(currentBlocks));
        return pages;
    }

    private static string RenderSectionPage(IReadOnlyList<string> blocks)
        => $"<div class=\"section-stack\">{string.Join("", blocks)}</div>";

    private static int SectionUnits(string content)
    {
        var lines = content
            .Replace("\r\n", "\n")
            .Split('\n')
            .Sum(line => Math.Max(1, (int)Math.Ceiling(line.Length / 88d)));
        return 5 + lines;
    }

    private static IReadOnlyList<string> SplitSectionContent(string content)
    {
        var chunks = new List<string>();
        foreach (var chunk in SplitContent(content, SectionPageCharacterLimit))
            AddSectionChunk(chunks, chunk);
        return chunks;
    }

    private static void AddSectionChunk(ICollection<string> chunks, string content)
    {
        if (SectionUnits(content) <= SectionPageUnitLimit || content.Length <= 1)
        {
            chunks.Add(content);
            return;
        }

        var splitAt = FindSectionSplit(content);
        AddSectionChunk(chunks, content[..splitAt].Trim());
        AddSectionChunk(chunks, content[splitAt..].Trim());
    }

    private static int FindSectionSplit(string content)
    {
        var midpoint = content.Length / 2;
        var splitAt = content.LastIndexOfAny(['\n', '.', ';', ' '], midpoint);
        return splitAt > content.Length / 4 ? splitAt + 1 : midpoint;
    }

    private string CoverPage(ProcedureDocumentSnapshot s, ProcedureDocumentReadiness readiness)
    {
        var logo = LogoHtml("cover-logo");
        var warning = readiness.IsReady
            ? "<div class=\"ready-banner\">Hồ sơ đã đủ điều kiện kiểm soát nội bộ</div>"
            : $"<div class=\"warning-banner\"><strong>Chưa đủ điều kiện ban hành:</strong> {H(string.Join("; ", readiness.MissingItems))}</div>";

        return $$"""
<div class="cover-brand">{{logo}}<div><div class="hospital-name">{{HospitalName}}</div><div class="hospital-unit">HỆ THỐNG QUẢN LÝ CHUYÊN MÔN</div></div></div>
<div class="document-category">QUY TRÌNH CHUYÊN MÔN</div>
<h1 class="cover-title">{{H(s.Procedure.Name)}}</h1>
<div class="document-code">Mã số: <strong>{{H(s.Procedure.ProcedureCode)}}</strong> • Phiên bản: <strong>{{H(s.Version.VersionLabel ?? $"v{s.Version.VersionNo:00}")}}</strong></div>
<table class="meta-table cover-meta">
<tr><th>Khoa/phòng chủ trì</th><td>{{H(s.DepartmentName)}}</td><th>Lần ban hành</th><td>{{H(s.Version.IssueNumber)}}</td></tr>
<tr><th>Ngày ban hành</th><td>{{DateOnly(s.Version.IssueDate)}}</td><th>Trạng thái</th><td>{{H(StatusLabel(s.Version.StatusCode))}}</td></tr>
</table>
{{warning}}
""";
    }

    private string SignatureBlock(ProcedureDocumentSnapshot s, string hash, bool standalone)
        => standalone
            ? SignatureGrid(s, hash)
            : $$"""
<h2 class="block-title signature-block-title">Xác nhận và phê duyệt nội bộ</h2>
<div class="signature-page signature-page-inline">
  {{SignatureGrid(s, hash)}}
</div>
""";

    private string SignatureApprovalPage(ProcedureDocumentSnapshot s, string hash)
        => $$"""
<div class="signature-page signature-page-standalone">
  <h1 class="section-title signature-page-title">XÁC NHẬN VÀ PHÊ DUYỆT NỘI BỘ</h1>
  <p class="signature-page-note">Chữ ký điện tử nội bộ được gắn với tài khoản, thời điểm ký và nội dung hiện hành của phiên bản.</p>
  {{SignatureGrid(s, hash)}}
</div>
""";

    private string SignatureGrid(ProcedureDocumentSnapshot s, string hash)
        => $$"""
<div class="signature-grid">
{{WriterSignCards(s, hash)}}
{{SignCard(s, "checker", "Người kiểm tra", hash)}}
{{SignCard(s, "approver", "Người phê duyệt", hash)}}
</div>
""";

    private string ControlPage(
        ProcedureDocumentSnapshot s,
        string currentHash,
        IEnumerable<ProcedureDistributionRecipient> recipients,
        IEnumerable<ProcedureRevisionEntry> revisions)
    {
        var authorities = string.Join("", new[]
        {
            AuthorityRow(s, "writer", "Người viết", "Soạn thảo và chịu trách nhiệm nội dung", currentHash),
            AuthorityRow(s, "checker", "Người kiểm tra", "Kiểm tra tính đầy đủ và phù hợp chuyên môn", currentHash),
            AuthorityRow(s, "approver", "Người phê duyệt", "Phê duyệt, ban hành và chịu trách nhiệm hiệu lực", currentHash)
        });
        var recipientSection = recipients.Any()
            ? $$"""
<h2 class="block-title">Nơi nhận và phân phối</h2>
<table class="print-avoid-break"><thead><tr><th>STT</th><th>Đơn vị nhận</th><th>Loại bản</th></tr></thead><tbody>{{RecipientRows(recipients)}}</tbody></table>
"""
            : string.Empty;
        var revisionSection = revisions.Any()
            ? $$"""
<h2 class="block-title">Theo dõi sửa đổi</h2>
<table class="print-avoid-break"><thead><tr><th>Lần</th><th>Ngày</th><th>Trang</th><th>Mục</th><th>Nội dung thay đổi</th></tr></thead><tbody>{{RevisionRows(revisions)}}</tbody></table>
"""
            : string.Empty;

        return $$"""
<h1 class="section-title">KIỂM SOÁT TÀI LIỆU</h1>
<table class="meta-table control-meta">
<tr><th>PDF nguồn</th><td colspan="3">{{H(s.Version.SourcePdfFileName)}}</td></tr>
<tr><th>Mã kiểm soát</th><td colspan="3">{{H(DocumentControlCode(s))}}</td></tr>
<tr><th>Lý do thay đổi</th><td colspan="3">{{H(s.Version.ChangeReason)}}</td></tr>
</table>
<h2 class="block-title">Phân công và thẩm quyền xác nhận</h2>
<table class="authority-table print-avoid-break"><thead><tr><th>Vai trò</th><th>Trách nhiệm</th><th>Người đảm nhận</th><th>Tình trạng</th></tr></thead><tbody>{{authorities}}</tbody></table>
{{recipientSection}}
{{revisionSection}}
""";
    }

    private static string RecipientContinuationPage(
        IEnumerable<ProcedureDistributionRecipient> recipients,
        int page,
        int total)
        => $$"""
<h1 class="section-title">NƠI NHẬN VÀ PHÂN PHỐI <span class="continuation">({{page}}/{{total}})</span></h1>
<table><thead><tr><th>STT</th><th>Đơn vị nhận</th><th>Loại bản</th></tr></thead><tbody>{{RecipientRows(recipients)}}</tbody></table>
""";

    private static string RevisionContinuationPage(
        IEnumerable<ProcedureRevisionEntry> revisions,
        int page,
        int total)
        => $$"""
<h1 class="section-title">THEO DÕI SỬA ĐỔI <span class="continuation">({{page}}/{{total}})</span></h1>
<table><thead><tr><th>Lần</th><th>Ngày</th><th>Trang</th><th>Mục</th><th>Nội dung thay đổi</th></tr></thead><tbody>{{RevisionRows(revisions)}}</tbody></table>
""";

    private static string RecipientRows(IEnumerable<ProcedureDistributionRecipient> recipients)
    {
        var rows = recipients
            .Select(item => $"<tr><td>{item.DisplayOrder}</td><td>{H(item.RecipientName)}</td><td>{(item.IsMarked ? "Bản kiểm soát" : "Bản tham khảo")}</td></tr>")
            .ToList();
        return rows.Count == 0
            ? "<tr><td colspan=\"3\" class=\"empty\">Chưa khai báo nơi nhận</td></tr>"
            : string.Join("", rows);
    }

    private static string RevisionRows(IEnumerable<ProcedureRevisionEntry> revisions)
    {
        var rows = revisions
            .Select(item => $"<tr><td>{item.DisplayOrder}</td><td>{D(item.RevisionDate)}</td><td>{H(item.PageRef)}</td><td>{H(item.SectionRef)}</td><td>{H(item.Summary)}</td></tr>")
            .ToList();
        return rows.Count == 0
            ? "<tr><td colspan=\"5\" class=\"empty\">Chưa có lịch sử sửa đổi</td></tr>"
            : string.Join("", rows);
    }

    private string FlowPage(IReadOnlyList<ProcedureStep> steps, int page, int total)
    {
        var rowParts = new List<string>();
        for (var index = 0; index < steps.Count; index++)
        {
            rowParts.Add(FlowStepRow(steps[index], index, steps.Count));
        }

        return $$"""
<h1 class="section-title flow-page-title">LƯU ĐỒ QUY TRÌNH <span class="continuation">({{page}}/{{total}})</span></h1>
{{(page > 1 ? "<div class=\"flow-page-link\">Tiếp từ trang lưu đồ trước</div>" : "")}}
<table class="flow-table">
<thead><tr><th>Trách nhiệm</th><th>Các bước thực hiện</th><th>Mô tả / Các biểu mẫu</th></tr></thead>
<tbody>{{string.Join("", rowParts)}}</tbody>
</table>
{{(page < total ? "<div class=\"flow-page-link flow-page-next\">Tiếp tục ở trang lưu đồ sau</div>" : "")}}
""";
    }

    private string FlowStepRow(ProcedureStep step, int index, int count)
    {
        var stepName = H(PrintableText(step.Name, "Chưa đặt tên bước"));
        var subtitle = FlowShapeSubtitle(step);
        var shape = Shape(step.FlowShapeCode);
        var isFirst = index == 0;
        var isLast = index == count - 1;
        
        var lineClass = isFirst && isLast ? "" 
                      : isFirst ? "flow-line-bottom-half" 
                      : isLast ? "flow-line-top-half" 
                      : "flow-line-full";
                      
        var arrowMarkup = isFirst ? "" : "<div class=\"flow-arrow-head\"></div>";

        return $$"""
<tr class="flow-table-row">
  <td class="flow-responsibility">{{FlowResponsibility(step)}}</td>
  <td class="flow-step-cell">
    <div class="flow-line-behind {{lineClass}}"></div>
    <div class="flow-symbol shape-{{shape}}">
      {{arrowMarkup}}
      <span><strong>{{stepName}}</strong>{{subtitle}}</span>
    </div>
  </td>
  <td class="flow-note">{{StepNote(step)}}</td>
</tr>
""";
    }

    private static string FlowLinkRow()
        => """
<tr class="flow-link-row">
  <td class="flow-link-pad"></td>
  <td class="flow-link-cell"><div class="flow-link-arrow" aria-hidden="true"></div></td>
  <td class="flow-link-pad"></td>
</tr>
""";

    private static string FlowShapeSubtitle(ProcedureStep step)
    {
        var details = new List<string>();
        if (!string.IsNullOrWhiteSpace(step.DetailSectionNumber))
            details.Add(step.DetailSectionNumber.Trim());

        var description = step.Description?.Trim();
        var name = step.Name?.Trim();
        if (!string.IsNullOrWhiteSpace(description) &&
            !string.Equals(description, name, StringComparison.OrdinalIgnoreCase))
            details.Add(Truncate(description, 120));

        return details.Count == 0
            ? ""
            : $"<small>{H(string.Join(" · ", details))}</small>";
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..(maxLength - 1)].TrimEnd() + "…";

    private string TraceabilityPage(
        ProcedureDocumentSnapshot snapshot,
        IEnumerable<ProcedureAttachment> attachments,
        IEnumerable<ProcedureSignoffRecord> signoffs,
        string hash,
        DateTime generatedAt)
    {
        return $$"""
<h1 class="section-title">HỒ SƠ VÀ KIỂM SOÁT BAN HÀNH</h1>
<h2 class="block-title">Tệp gắn kèm</h2>
<table><thead><tr><th>STT</th><th>Tên tệp</th><th>Loại</th><th>Dung lượng</th><th>Ghi chú</th></tr></thead><tbody>{{AttachmentRows(attachments)}}</tbody></table>
<h2 class="block-title">Nhật ký chữ ký nội bộ</h2>
<table class="signoff-log"><thead><tr><th>STT</th><th>Vai trò</th><th>Người xác nhận</th><th>Thời điểm</th><th>Hiệu lực</th></tr></thead><tbody>{{SignoffRows(snapshot, signoffs, hash)}}</tbody></table>
<div class="trace-box"><strong>Thông tin bản in</strong><div>Bản in phục vụ kiểm soát và ban hành nội bộ.</div><div>Thời điểm tạo bản in: {{D(generatedAt)}}</div></div>
""";
    }

    private string AttachmentContinuationPage(
        IEnumerable<ProcedureAttachment> attachments,
        int page,
        int total)
        => $$"""
<h1 class="section-title">TỆP GẮN KÈM <span class="continuation">({{page}}/{{total}})</span></h1>
<table><thead><tr><th>STT</th><th>Tên tệp</th><th>Loại</th><th>Dung lượng</th><th>Ghi chú</th></tr></thead><tbody>{{AttachmentRows(attachments)}}</tbody></table>
""";

    private string SignoffContinuationPage(
        ProcedureDocumentSnapshot snapshot,
        IEnumerable<ProcedureSignoffRecord> signoffs,
        string hash,
        int page,
        int total)
        => $$"""
<h1 class="section-title">NHẬT KÝ CHỮ KÝ NỘI BỘ <span class="continuation">({{page}}/{{total}})</span></h1>
<table class="signoff-log"><thead><tr><th>STT</th><th>Vai trò</th><th>Người xác nhận</th><th>Thời điểm</th><th>Hiệu lực</th></tr></thead><tbody>{{SignoffRows(snapshot, signoffs, hash)}}</tbody></table>
""";

    private string AttachmentRows(IEnumerable<ProcedureAttachment> attachments)
    {
        var rows = attachments
            .Select((item, index) => $"<tr><td>{index + 1}</td><td>{AttachmentLinkHtml(item)}</td><td>{H(AttachmentLabel(item.AttachmentType))}</td><td>{FileSize(item.FileSizeBytes)}</td><td>{AttachmentNote(item)}</td></tr>")
            .ToList();
        return rows.Count == 0
            ? "<tr><td colspan=\"5\" class=\"empty\">Không có tệp đính kèm</td></tr>"
            : string.Join("", rows);
    }

    private string AttachmentLinkHtml(ProcedureAttachment attachment)
    {
        if (string.IsNullOrWhiteSpace(_publicBaseUrl))
            return H(attachment.FileName);

        var url = _accessTokens.CreateDownloadUrl(_publicBaseUrl, attachment.ProcedureAttachmentId, _generatedAtUtc);
        return $"<a class=\"file-download-link\" href=\"{H(url)}\" title=\"Tải {H(attachment.FileName)}\">{H(attachment.FileName)}<span class=\"file-download-mark\" aria-hidden=\"true\">↓</span></a>";
    }

    private string SignoffRows(ProcedureDocumentSnapshot snapshot, IEnumerable<ProcedureSignoffRecord> signoffs, string hash)
    {
        var rows = signoffs.Select((sign, index) =>
        {
            var isCurrent = !_snapshots.IsSignoffStale(snapshot, sign);
            return $"<tr><td>{index + 1}</td><td>{H(RoleLabel(sign.SignoffRole))}</td><td>{H(sign.SignerFullName ?? sign.SignerUsername)}</td><td>{D(sign.SignedAt)}</td><td><span class=\"sign-state {(isCurrent ? "is-current" : "is-stale")}\">{(isCurrent ? "Còn hiệu lực" : "Hết hiệu lực")}</span></td></tr>";
        }).ToList();
        return rows.Count == 0
            ? "<tr><td colspan=\"5\" class=\"empty\">Chưa có xác nhận nội bộ</td></tr>"
            : string.Join("", rows);
    }

    private string RenderPage(ProcedureDocumentSnapshot s, string content, int page, int total, string versionLabel)
        => $$"""
<section class="page">
  <header class="page-header"><div class="mini-brand">{{LogoHtml("mini-logo")}}<div><strong>{{HospitalName}}</strong><span>QUẢN LÝ CHUYÊN MÔN</span></div></div><div class="page-document"><strong>{{H(s.Procedure.ProcedureCode)}}</strong><span>{{H(versionLabel)}}</span></div></header>
  <main class="page-main">{{content}}</main>
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
        if (sign is null) return EmptySignCard(label, "Chưa ký");
        var image = IsSupportedSignatureImage(sign.SignatureImageDataUrl)
            ? $"<img class=\"signature-image\" src=\"{H(sign.SignatureImageDataUrl)}\" alt=\"Chữ ký {H(label)}\">"
            : "<div class=\"signature-space signed-mark\">ĐÃ XÁC NHẬN</div>";
        return SignedSignCard(label, sign.SignerFullName ?? sign.SignerUsername, sign.SignerUsername, D(sign.SignedAt), image);
    }

    private string WriterSignCards(ProcedureDocumentSnapshot snapshot, string currentHash)
    {
        var cards = new List<string>();
        var requiredCount = Math.Max(1, snapshot.Version.RequiredWriterSignatures);
        var writers = _snapshots.GetOrderedWriterAssignments(snapshot).Take(requiredCount).ToList();
        for (var index = 0; index < requiredCount; index++)
        {
            var assigned = writers.ElementAtOrDefault(index)
                ?? snapshot.WriterAssignments.OrderBy(item => item.DisplayOrder).ElementAtOrDefault(index);
            var label = requiredCount == 1 ? "Người viết" : $"Người viết {index + 1}";
            if (assigned is null)
            {
                cards.Add(EmptySignCard(label, "Chưa ký"));
                continue;
            }

            var sign = _snapshots.GetWriterSignoffForDisplay(snapshot, assigned.AssignedUserId);
            if (!_snapshots.IsWriterEffectivelySigned(snapshot, assigned.AssignedUserId) || sign is null)
            {
                var expected = $"Chờ {H(assigned.AssignedFullName ?? assigned.AssignedUsername)} ký";
                cards.Add(EmptySignCard(label, expected));
                continue;
            }

            var image = IsSupportedSignatureImage(sign.SignatureImageDataUrl)
                ? $"<img class=\"signature-image\" src=\"{H(sign.SignatureImageDataUrl)}\" alt=\"Chữ ký {H(label)}\">"
                : "<div class=\"signature-space signed-mark\">ĐÃ XÁC NHẬN</div>";
            cards.Add(SignedSignCard(label, sign.SignerFullName ?? sign.SignerUsername, sign.SignerUsername, D(sign.SignedAt), image));
        }

        return string.Join("", cards);
    }

    private static string EmptySignCard(string label, string status)
        => $$"""
<div class="signature-card is-empty">
  <div class="signature-card-head">
    <span class="signature-role">{{H(label)}}</span>
    <span class="signature-badge is-pending">Chờ xác nhận</span>
  </div>
  <div class="signature-card-body">
    <div class="signature-space"></div>
    <div class="signature-placeholder">{{status}}</div>
  </div>
  <div class="signature-card-foot">
    <strong>Chưa có người ký</strong>
    <span class="sign-account">Ký điện tử nội bộ sẽ hiển thị tại đây</span>
    <span class="sign-time"> </span>
  </div>
</div>
""";

    private static string SignedSignCard(string label, string? signerName, string? signerUsername, string signedAt, string signatureMarkup)
        => $$"""
<div class="signature-card is-signed">
  <div class="signature-card-head">
    <span class="signature-role">{{H(label)}}</span>
    <span class="signature-badge is-signed">Đã xác nhận</span>
  </div>
  <div class="signature-card-body">
    {{signatureMarkup}}
  </div>
  <div class="signature-card-foot">
    <strong>{{H(signerName)}}</strong>
    <span class="sign-account">Tài khoản: {{H(signerUsername)}}</span>
    <span class="sign-time">Ký lúc: {{H(signedAt)}}</span>
  </div>
</div>
""";

    private string AuthorityRow(ProcedureDocumentSnapshot s, string role, string label, string responsibility, string currentHash)
    {
        if (role == "writer")
        {
            var requiredCount = Math.Max(1, s.Version.RequiredWriterSignatures);
            var assignments = _snapshots.GetOrderedWriterAssignments(s).Take(requiredCount).ToList();
            var signedCount = _snapshots.GetEffectiveWriterSignatureCount(s);
            var signer = signedCount == 0
                ? string.Join("; ", assignments.Select(item => item.AssignedFullName ?? item.AssignedUsername ?? item.AssignedUserId.ToString()))
                : string.Join("; ", assignments.Select(item =>
                {
                    if (_snapshots.IsWriterEffectivelySigned(s, item.AssignedUserId))
                    {
                        var sign = _snapshots.GetWriterSignoffForDisplay(s, item.AssignedUserId);
                        if (sign is not null)
                            return sign.SignerFullName ?? sign.SignerUsername ?? "Tài khoản nội bộ";
                    }

                    return item.AssignedFullName ?? item.AssignedUsername ?? item.AssignedUserId.ToString();
                }));
            var status = signedCount == 0
                ? "Chưa ký"
                : $"Đã ký {signedCount}/{requiredCount}";
            return $"<tr><td><strong>{H(label)}</strong></td><td>{H(responsibility)}</td><td>{H(signer)}</td><td>{H(status)}</td></tr>";
        }

        var currentSigns = s.Signoffs
            .Where(item => item.SignoffRole == role && item.ContentHashSha256.Equals(currentHash, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.SignedAt)
            .ToList();
        var roleSigner = currentSigns.FirstOrDefault() is { } firstSign
            ? firstSign.SignerFullName ?? firstSign.SignerUsername ?? "Tài khoản nội bộ"
            : "Chưa xác định";
        var roleStatus = currentSigns.Count == 0
            ? "Chưa ký"
            : $"Đã ký {D(currentSigns[0].SignedAt)}";
        return $"<tr><td><strong>{H(label)}</strong></td><td>{H(responsibility)}</td><td>{H(roleSigner)}</td><td>{H(roleStatus)}</td></tr>";
    }

    private static bool IsSupportedSignatureImage(string? value)
        => ProcedureSignoffService.IsValidSignatureImage(value);

    private static IReadOnlyList<IReadOnlyList<ProcedureStep>> BuildFlowGroups(IReadOnlyList<ProcedureStep> steps)
    {
        var groups = new List<IReadOnlyList<ProcedureStep>>();
        var current = new List<ProcedureStep>();
        var currentUnits = 0;
        foreach (var sourceStep in steps)
        {
            foreach (var step in ExpandFlowStep(sourceStep))
            {
                var units = FlowUnits(step);
                if (current.Count > 0 && currentUnits + units > FlowPageUnitLimit)
                {
                    groups.Add(current.ToList());
                    current.Clear();
                    currentUnits = 0;
                }
                current.Add(step);
                currentUnits += units;
            }
        }
        if (current.Count > 0) groups.Add(current);
        return groups;
    }

    private static int FlowUnits(ProcedureStep step)
    {
        var responsibilityLines = WrappedLines(step.ResponsibilityText, 32);
        var formLines = WrappedLines(step.FormReferenceText, 34);
        var contentLines = WrappedLines($"{step.Name}\n{step.Description}", 40);
        return 3 + Math.Max(contentLines, Math.Max(responsibilityLines, formLines));
    }

    private static int WrappedLines(string? value, int charactersPerLine)
        => string.IsNullOrWhiteSpace(value)
            ? 0
            : value.Replace("\r\n", "\n")
                .Split('\n')
                .Sum(line => Math.Max(1, (int)Math.Ceiling(line.Length / (double)charactersPerLine)));

    private static IReadOnlyList<ProcedureStep> ExpandFlowStep(ProcedureStep step)
    {
        if (string.IsNullOrWhiteSpace(step.Description) ||
            step.Description.Length <= FlowDescriptionCharacterLimit)
            return [step];

        var chunks = SplitContent(step.Description, FlowDescriptionCharacterLimit);
        return chunks.Select((description, index) => step with
        {
            Name = index == 0 ? step.Name : $"{step.Name} (tiếp)",
            Description = description,
            FormReferenceText = index == 0 ? step.FormReferenceText : null
        }).ToList();
    }



    private string StepNote(ProcedureStep step)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(step.DetailSectionNumber))
            parts.Add($"<strong>{H(step.DetailSectionNumber.Trim())}</strong>");
        if (!string.IsNullOrWhiteSpace(step.FormReferenceText))
            parts.Add(Lines(step.FormReferenceText, ""));
        if (!string.IsNullOrWhiteSpace(step.Description))
            parts.Add(Lines(step.Description, ""));
        var linkedAssignments = _snapshots.GetSnapshot(step.ProcedureVersionId).StepAttachmentAssignments
            .Where(item => item.ProcedureStepId == step.ProcedureStepId)
            .OrderBy(item => item.DisplayOrder)
            .Select(item => item.ProcedureAttachmentId)
            .ToList();
        if (linkedAssignments.Count > 0)
        {
            var linkedItems = linkedAssignments
                .Where(id => _attachmentIndex.ContainsKey(id))
                .Select(id => $"<div class=\"flow-form-lines\">{AttachmentLinkHtml(_attachmentIndex[id])}</div>");
            parts.Add(string.Join("", linkedItems));
        }
        else if (step.FormAttachmentId is { } attachmentId && _attachmentIndex.TryGetValue(attachmentId, out var linked))
            parts.Add($"<div class=\"flow-form-lines\">{AttachmentLinkHtml(linked)}</div>");
        return parts.Count == 0 ? "<span class=\"empty\">Chưa khai báo mô tả/biểu mẫu</span>" : string.Join("<br>", parts);
    }

    private string FlowResponsibility(ProcedureStep step)
    {
        var snapshot = _snapshots.GetSnapshot(step.ProcedureVersionId);
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(step.ResponsibilityText))
        {
            foreach (var line in step.ResponsibilityText.Replace("\r\n", "\n").Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.Length > 0)
                    lines.Add(trimmed.StartsWith('-') ? trimmed : $"- {trimmed}");
            }
        }

        var roleNames = snapshot.StepRoleAssignments
            .Where(item => item.ProcedureStepId == step.ProcedureStepId)
            .OrderBy(item => item.DisplayOrder)
            .Select(item => _snapshots.GetRoleName(item.RoleId))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        foreach (var roleName in roleNames)
        {
            var formatted = $"- {roleName}";
            if (!lines.Contains(formatted, StringComparer.OrdinalIgnoreCase))
                lines.Add(formatted);
        }

        var locationNames = snapshot.StepLocationAssignments
            .Where(item => item.ProcedureStepId == step.ProcedureStepId)
            .OrderBy(item => item.DisplayOrder)
            .Select(item => _snapshots.GetDepartmentName(item.DepartmentId))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        foreach (var locationName in locationNames)
        {
            var formatted = $"- {locationName}";
            if (!lines.Contains(formatted, StringComparer.OrdinalIgnoreCase))
                lines.Add(formatted);
        }

        return lines.Count == 0
            ? "—"
            : string.Join("<br>", lines.Select(H));
    }

    private static string Lines(string? value, string fallback)
        => string.Join("<br>", PrintableText(value, fallback).Split('\n').Select(H));

    private static string PrintableText(string? value, string fallback = "Chưa có nội dung.")
    {
        var text = value?.Replace("\r\n", "\n").Trim();
        if (string.IsNullOrWhiteSpace(text)) return fallback;
        return text.Contains("OCR_PENDING", StringComparison.OrdinalIgnoreCase)
            ? "Nội dung chi tiết đang chờ trích xuất và đối chiếu từ PDF scan nguồn trước khi ban hành."
            : text;
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
    private static string DateOnly(DateTime? value) => value?.ToLocalTime().ToString("dd/MM/yyyy") ?? "";
    private static string Shape(string? shape) => shape is "terminator" or "decision" or "data" or "document" ? shape : "process";
    private static string StatusLabel(string status) => status switch { "draft" => "Bản nháp", "pending_approval" => "Chờ phê duyệt", "active" => "Đang hiệu lực", "superseded" => "Đã được thay thế", "archived" => "Lưu trữ", "rejected" => "Bị từ chối", _ => status };
    private static string RoleLabel(string role) => role switch { "writer" => "Người viết", "checker" => "Người kiểm tra", "approver" => "Người phê duyệt", _ => role };
    private static string AttachmentLabel(string type) => type == "source_pdf" ? "PDF nguồn" : "Tệp đính kèm";
    private static string AttachmentNote(ProcedureAttachment attachment)
        => attachment.AttachmentType == "source_pdf"
            ? "Tài liệu nguồn được lưu trong hồ sơ kiểm soát"
            : "Tệp tham chiếu";
    private static string DocumentControlCode(ProcedureDocumentSnapshot snapshot)
    {
        var version = snapshot.Version.VersionLabel ?? $"v{snapshot.Version.VersionNo:00}";
        var issue = snapshot.Version.IssueNumber?.ToString() ?? "chưa ban hành";
        return $"{snapshot.Procedure.ProcedureCode} - {version} - Lần ban hành {issue}";
    }
    private static string FileSize(long? bytes) => bytes.HasValue ? $"{bytes.Value / 1024d / 1024d:0.##} MB" : "";

    private const string PrintCss = """
@page{size:A4;margin:0}*{box-sizing:border-box}html,body{margin:0;padding:0}body{font-family:"Times New Roman",serif;color:#111;background:#d7dce2;font-size:12.5pt;line-height:1.42}.print-toolbar{position:sticky;top:0;z-index:20;display:flex;align-items:center;justify-content:center;gap:16px;padding:10px;background:#10263d;color:#fff;font-family:Arial,sans-serif}.print-toolbar button{border:0;border-radius:4px;background:#69be45;color:#10263d;font-weight:700;padding:9px 18px;cursor:pointer}.page{width:210mm;height:297mm;margin:12px auto;background:#fff;padding:10mm 14mm 12mm;display:flex;flex-direction:column;overflow:hidden;box-shadow:0 5px 22px #0002;break-after:page;page-break-after:always}.page:last-child{break-after:auto;page-break-after:auto}.page-header{height:19mm;flex:0 0 19mm;border-bottom:1.5px solid #1f5ea8;display:flex;align-items:center;justify-content:space-between;padding-bottom:3mm}.mini-brand{display:flex;align-items:center;gap:3mm}.mini-brand div{display:flex;flex-direction:column}.mini-brand strong{font-size:10pt;color:#1f5ea8}.mini-brand span{font:7.5pt Arial,sans-serif;color:#555}.mini-logo{width:13mm;height:13mm;object-fit:contain}.page-document{text-align:right;display:flex;flex-direction:column}.page-document strong{color:#1f5ea8}.page-document span{font-size:10pt}.page-main{flex:1;min-height:0;max-height:100%;overflow:hidden;padding-top:5mm;display:block}.page-footer{flex:0 0 auto;border-top:1px solid #777;padding-top:2.5mm;margin-top:4mm;display:flex;justify-content:space-between;font-size:9pt;color:#555}.cover-brand{display:flex;align-items:center;justify-content:center;gap:6mm;margin-top:4mm;text-align:left}.cover-logo{width:28mm;height:28mm;object-fit:contain}.hospital-name{font-weight:700;color:#1f5ea8;font-size:17pt}.hospital-unit{font:8.5pt Arial,sans-serif;letter-spacing:.08em;color:#555}.document-category{text-align:center;margin-top:6mm;color:#1f5ea8;font-weight:700;letter-spacing:.14em;font-size:11pt}.cover-title{text-align:center;text-transform:uppercase;font-size:20pt;line-height:1.22;margin:4mm 8mm 3mm}.document-code{text-align:center;margin-bottom:4mm;font-size:11pt}.cover-meta{margin-bottom:2mm}.section-stack{display:flex;flex-direction:column}.procedure-section+.procedure-section{margin-top:7mm}.procedure-section .section-title{margin-bottom:4mm}.section-title{font-size:17pt;color:#1f5ea8;border-bottom:2px solid #69be45;padding-bottom:3mm;margin:0 0 8mm}.flow-page-title{padding-bottom:2mm;margin-bottom:2mm}.continuation{font-size:10pt;color:#555;font-weight:400}.block-title{font-size:13pt;color:#1f5ea8;margin:5mm 0 2mm}.section-body{white-space:pre-wrap;text-align:justify;font-size:12.5pt;line-height:1.55}.meta-table,table{width:100%;border-collapse:collapse;margin:2mm 0}th,td{border:1px solid #555;padding:2mm 2.4mm;vertical-align:top}th{background:#eaf1f8;text-align:left;color:#163f6d}.meta-table th{width:20%}.authority-table{font-size:10pt}.authority-table th:nth-child(1){width:17%}.authority-table th:nth-child(2){width:37%}.authority-table th:nth-child(3){width:24%}.print-avoid-break,.signature-page,.block-title+table{break-inside:avoid;page-break-inside:avoid}.warning-banner{background:#fff4e5;border-color:#b45309}.ready-banner{background:#edf8e8;border-color:#4b9b2f}.warning-banner,.ready-banner{padding:2.5mm 3.5mm;margin:3mm 0;border-left:4px solid;font-size:10.5pt;line-height:1.35}.signature-page{display:block}.signature-page-inline{margin-top:1mm}.signature-page-standalone{display:flex;flex-direction:column;gap:2mm}.signature-block-title{margin-top:2mm;margin-bottom:1mm}.signature-page-title{margin-bottom:1mm;padding-bottom:2mm}.signature-page-note{margin:0 0 2mm;color:#555;font-size:10pt;line-height:1.4}.signature-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:3mm;margin-top:1mm;break-inside:avoid;page-break-inside:avoid}.signature-card{border:1px solid #8da4bf;min-height:46mm;display:flex;flex-direction:column;background:linear-gradient(180deg,#fff 0%,#f8fbff 100%);break-inside:avoid;page-break-inside:avoid}.signature-card-head{display:flex;align-items:center;justify-content:space-between;gap:2mm;padding:2.4mm 3mm;border-bottom:1px solid #cad6e3;background:#eef4fb}.signature-role{font-weight:700;color:#163f6d;font-size:10.5pt}.signature-badge{display:inline-flex;align-items:center;justify-content:center;padding:1mm 2.2mm;border-radius:999px;font:700 7.6pt Arial,sans-serif;letter-spacing:.02em;border:1px solid}.signature-badge.is-signed{color:#2f7f22;border-color:#69be45;background:#edf8e8}.signature-badge.is-pending{color:#9a3412;border-color:#d6a45d;background:#fff4e5}.signature-card-body{flex:1;display:flex;align-items:center;justify-content:center;padding:1.5mm 2.5mm 1mm}.signature-space{height:17mm;width:100%;border-bottom:1px dashed #b8c4d1;display:flex;align-items:center;justify-content:center}.signature-placeholder{margin-top:1mm;color:#7a5a20;font-style:italic;text-align:center;font-size:8.5pt;line-height:1.3}.signed-mark{font:700 8.5pt Arial,sans-serif;color:#2f7f22;border:1px solid #69be45;background:#edf8e8;padding:1.5mm 3mm}.signature-image{width:100%;height:16mm;object-fit:contain;filter:brightness(0) contrast(10)}.signature-card-foot{padding:1.5mm 2.5mm 2mm;border-top:1px solid #d9e2ec;text-align:center;display:flex;flex-direction:column;gap:.4mm;min-height:13mm}.signature-card-foot strong{color:#163f6d;font-size:10pt;line-height:1.2}.signature-card-foot span{font-size:8.4pt}.sign-account{color:#555}.sign-time{color:#4a5568}.signature-card.is-empty .signature-card-foot strong{color:#6b7280}.empty{color:#777;font-style:italic;text-align:center}.sign-state{display:inline-block;font:700 8pt Arial,sans-serif;padding:1mm 1.5mm;border:1px solid}.sign-state.is-current{color:#2f7f22;border-color:#69be45;background:#edf8e8}.sign-state.is-stale{color:#9a3412;border-color:#c2410c;background:#fff4e5}.signoff-log{font-size:9pt}.flow-table{table-layout:fixed;width:100%;margin-top:2mm;font-size:10pt;line-height:1.32;border:1px solid #111;border-collapse:collapse}.flow-table th,.flow-table td{border:1px solid #111;padding:2.5mm 2.2mm;vertical-align:middle}.flow-table th{text-align:center;font-weight:700;background:#fff;color:#111;font-size:10pt}.flow-table th:nth-child(1){width:30%}.flow-table th:nth-child(2){width:32%}.flow-table th:nth-child(3){width:38%}.flow-table-row{break-inside:avoid;page-break-inside:avoid}.flow-responsibility,.flow-note{text-align:left;font-size:10pt;line-height:1.35;vertical-align:middle}.flow-step-cell{position:relative;text-align:center;vertical-align:middle;padding:4mm 2mm;z-index:1}.flow-line-behind{position:absolute;left:50%;width:1px;background:#111;z-index:-1}.flow-line-top-half{top:0;bottom:50%}.flow-line-bottom-half{top:50%;bottom:0}.flow-line-full{top:0;bottom:0}.flow-arrow-head{position:absolute;top:-2mm;left:50%;transform:translateX(-50%);border-left:1.5mm solid transparent;border-right:1.5mm solid transparent;border-top:2mm solid #111;z-index:3}.flow-symbol{position:relative;z-index:2;display:inline-block;max-width:44mm;min-width:20mm;padding:2.2mm 4mm;border:1px solid #111;background:#fff;color:#111;font-size:9.5pt;line-height:1.25;text-align:center;word-break:break-word}.flow-symbol>span{display:block}.flow-symbol strong{display:block;font-size:9.5pt;font-weight:700;line-height:1.2}.flow-symbol small{display:block;margin-top:.7mm;font-size:8pt;font-weight:400;line-height:1.2;color:#222}.shape-terminator{border-radius:999px;padding:2.4mm 5.5mm}.shape-process{border-radius:0}.shape-decision{border:none;background:transparent;padding:0;margin:0 auto;width:30mm;height:30mm;display:inline-flex;align-items:center;justify-content:center;position:relative}.shape-decision::before{content:"";position:absolute;left:50%;top:50%;width:21.2mm;height:21.2mm;background:#fff;border:1px solid #111;transform:translate(-50%,-50%) rotate(45deg);z-index:-1}.shape-decision>span{position:relative;z-index:1;display:block;max-width:15mm;font-size:8.5pt;line-height:1.15}.shape-data{position:relative;border:none;background:transparent;margin:0 auto;width:fit-content;max-width:44mm;min-width:20mm}.shape-data::before{content:"";position:absolute;inset:0;background:#fff;border:1px solid #111;transform:skewX(-12deg);z-index:-1}.shape-data>span{position:relative;z-index:1;display:block;padding:3mm 5mm}.shape-document{border-radius:0 0 4mm 4mm;padding-bottom:2.8mm}.flow-form-lines{margin-top:.5mm}.flow-page-link{text-align:center;color:#555;font-size:8.5pt;font-style:italic;margin-bottom:1mm}.flow-page-next{margin-top:1mm}.trace-box{border:1.5px solid #1f5ea8;background:#f5f9fd;padding:4mm;margin-top:6mm}.file-download-link{color:#1f5ea8;text-decoration:none;border-bottom:1px dotted #1f5ea8;font-size:inherit}.file-download-link:hover{color:#163f6d;border-bottom-style:solid}.file-download-mark{margin-left:1mm;font-size:.82em;opacity:.75}.logo-fallback{border:1px solid #1f5ea8;display:flex;align-items:center;justify-content:center;color:#1f5ea8;font-weight:700}@media print{body{background:#fff}.no-print{display:none!important}.page{margin:0;box-shadow:none}.file-download-link{color:#1f5ea8;text-decoration:underline}}
""";
}
