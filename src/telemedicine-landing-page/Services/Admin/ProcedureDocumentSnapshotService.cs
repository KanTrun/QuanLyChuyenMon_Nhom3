using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin;

public sealed class ProcedureDocumentSnapshotService
{
    private static readonly string[] RequiredSectionKinds =
    [
        "purpose", "scope", "basis", "definitions", "responsibilities",
        "procedure", "flowchart", "records", "appendices"
    ];

    private readonly IMedDataStore _store;

    public ProcedureDocumentSnapshotService(IMedDataStore store)
    {
        _store = store;
    }

    public ProcedureDocumentSnapshot GetSnapshot(Guid versionId)
    {
        var version = _store.ProcedureVersions.First(v => v.ProcedureVersionId == versionId);
        var procedure = _store.Procedures.First(p => p.ProcedureId == version.ProcedureId);
        var departmentId = version.DepartmentId ?? procedure.OwnerDepartmentId;
        var departmentName = _store.Departments
            .FirstOrDefault(item => item.DepartmentId == departmentId)?.Name;
        return new ProcedureDocumentSnapshot(
            procedure,
            version,
            departmentName,
            _store.ProcedureDocumentSections.Where(s => s.ProcedureVersionId == versionId).OrderBy(s => s.SectionOrder).ToList(),
            _store.ProcedureDistributionRecipients.Where(r => r.ProcedureVersionId == versionId).OrderBy(r => r.DisplayOrder).ToList(),
            _store.ProcedureRevisionEntries.Where(r => r.ProcedureVersionId == versionId).OrderBy(r => r.DisplayOrder).ToList(),
            _store.ProcedureSteps.Where(s => s.ProcedureVersionId == versionId).OrderBy(s => s.StepNo).ToList(),
            _store.ProcedureAttachments.Where(a => a.ProcedureVersionId == versionId).OrderBy(a => a.FileName).ToList(),
            _store.ProcedureSignoffRecords.Where(s => s.ProcedureVersionId == versionId).OrderBy(s => s.DisplayOrder).ThenByDescending(s => s.SignedAt).ToList());
    }

    public string ComputeContentHash(Guid versionId)
    {
        var snapshot = GetSnapshot(versionId);
        var canonical = new
        {
            Procedure = new { snapshot.Procedure.ProcedureCode, snapshot.Procedure.Name, snapshot.Procedure.ProcedureType, snapshot.Procedure.OwnerDepartmentId, snapshot.Procedure.Description },
            Version = new { snapshot.Version.VersionNo, snapshot.Version.VersionLabel, snapshot.Version.Title, snapshot.Version.Summary, snapshot.Version.ChangeReason, snapshot.Version.DepartmentId, snapshot.Version.IssueDate, snapshot.Version.IssueNumber, snapshot.Version.SourcePdfFileName, snapshot.Version.SourcePdfChecksumSha256 },
            Sections = snapshot.Sections.Select(s => new { s.SectionOrder, s.SectionNumber, s.Title, s.SectionKind, s.ContentText, s.IsRequired }),
            Recipients = snapshot.Recipients.Select(r => new { r.DisplayOrder, r.RecipientName, r.IsMarked }),
            Revisions = snapshot.Revisions.Select(r => new { r.DisplayOrder, r.RevisionDate, r.PageRef, r.SectionRef, r.Summary }),
            Steps = snapshot.Steps.Select(s => new { s.StepNo, s.StepCode, s.Name, s.Description, s.ResponsibilityText, s.FlowShapeCode, s.FormReferenceText, s.DetailSectionNumber, s.StandardDurationMinutes, s.IsRequired }),
            Attachments = snapshot.Attachments.Select(a => new { a.AttachmentType, a.FileName, a.FileUri, a.MimeType, a.FileSizeBytes, a.ChecksumSha256 })
        };
        var json = JsonSerializer.Serialize(canonical, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }

    public ProcedureDocumentReadiness CheckReadiness(Guid versionId, bool requireSignoffs)
    {
        var snapshot = GetSnapshot(versionId);
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(snapshot.Procedure.ProcedureCode)) missing.Add("Mã quy trình");
        if (string.IsNullOrWhiteSpace(snapshot.Version.Title)) missing.Add("Tiêu đề phiên bản");
        if (snapshot.Version.IssueDate is null) missing.Add("Ngày ban hành");
        if (snapshot.Version.IssueNumber is null) missing.Add("Lần ban hành");
        if (string.IsNullOrWhiteSpace(snapshot.Version.SourcePdfFileName)) missing.Add("Tên PDF nguồn");
        if (string.IsNullOrWhiteSpace(snapshot.Version.SourcePdfChecksumSha256)) missing.Add("Checksum PDF nguồn");
        if (snapshot.Attachments.Count == 0) missing.Add("Tệp đính kèm PDF nguồn");
        if (snapshot.Steps.Count == 0) missing.Add("Nội dung các bước quy trình");
        if (snapshot.Recipients.Count == 0) missing.Add("Nơi nhận");
        if (snapshot.Revisions.Count == 0) missing.Add("Bảng theo dõi sửa đổi");

        foreach (var kind in RequiredSectionKinds)
        {
            var section = snapshot.Sections.FirstOrDefault(s => string.Equals(s.SectionKind, kind, StringComparison.OrdinalIgnoreCase));
            if (section is null || (section.IsRequired && string.IsNullOrWhiteSpace(section.ContentText)))
            {
                missing.Add($"Mục {kind}");
            }
        }

        if (snapshot.Sections.Any(s => s.ContentText?.Contains("OCR_PENDING", StringComparison.OrdinalIgnoreCase) == true))
        {
            missing.Add("OCR đầy đủ từng trang PDF");
        }

        if (requireSignoffs)
        {
            foreach (var role in ProcedureSignoffService.RequiredRoles)
            {
                if (!HasCurrentSignoff(snapshot, role))
                    missing.Add($"Chữ ký {role}");
            }
        }

        return new ProcedureDocumentReadiness(missing.Count == 0, missing);
    }

    public bool HasCurrentSignoff(ProcedureDocumentSnapshot snapshot, string role)
    {
        var hash = ComputeContentHash(snapshot.Version.ProcedureVersionId);
        return snapshot.Signoffs.Any(s =>
            string.Equals(s.SignoffRole, role, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(s.ContentHashSha256, hash, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed record ProcedureDocumentSnapshot(
    ProfessionalProcedure Procedure,
    ProcedureVersion Version,
    string? DepartmentName,
    IReadOnlyList<ProcedureDocumentSection> Sections,
    IReadOnlyList<ProcedureDistributionRecipient> Recipients,
    IReadOnlyList<ProcedureRevisionEntry> Revisions,
    IReadOnlyList<ProcedureStep> Steps,
    IReadOnlyList<ProcedureAttachment> Attachments,
    IReadOnlyList<ProcedureSignoffRecord> Signoffs);

public sealed record ProcedureDocumentReadiness(bool IsReady, IReadOnlyList<string> MissingItems);
