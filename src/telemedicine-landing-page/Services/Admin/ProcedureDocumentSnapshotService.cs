using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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
        var stepIds = _store.ProcedureSteps
            .Where(s => s.ProcedureVersionId == versionId)
            .OrderBy(s => s.StepNo)
            .Select(s => s.ProcedureStepId)
            .ToHashSet();
        return new ProcedureDocumentSnapshot(
            procedure,
            version,
            departmentName,
            _store.ProcedureVersionAuthorAssignments
                .Where(item => item.ProcedureVersionId == versionId)
                .OrderBy(item => item.DisplayOrder)
                .ToList(),
            _store.ProcedureDocumentSections.Where(s => s.ProcedureVersionId == versionId).OrderBy(s => s.SectionOrder).ToList(),
            _store.ProcedureDistributionRecipients.Where(r => r.ProcedureVersionId == versionId).OrderBy(r => r.DisplayOrder).ToList(),
            _store.ProcedureRevisionEntries.Where(r => r.ProcedureVersionId == versionId).OrderBy(r => r.DisplayOrder).ToList(),
            _store.ProcedureSteps.Where(s => s.ProcedureVersionId == versionId).OrderBy(s => s.StepNo).ToList(),
            _store.ProcedureStepRoleAssignments
                .Where(item => stepIds.Contains(item.ProcedureStepId))
                .OrderBy(item => item.DisplayOrder)
                .ToList(),
            _store.ProcedureStepLocationAssignments
                .Where(item => stepIds.Contains(item.ProcedureStepId))
                .OrderBy(item => item.DisplayOrder)
                .ToList(),
            _store.ProcedureStepAttachmentAssignments
                .Where(item => stepIds.Contains(item.ProcedureStepId))
                .OrderBy(item => item.DisplayOrder)
                .ToList(),
            _store.ProcedureAttachments.Where(a => a.ProcedureVersionId == versionId).OrderBy(a => a.FileName).ToList(),
            _store.ProcedureSignoffRecords.Where(s => s.ProcedureVersionId == versionId).OrderBy(s => s.DisplayOrder).ThenByDescending(s => s.SignedAt).ToList(),
            _store.ProcedureVersionSnapshots
                .Where(item => item.ProcedureVersionId == versionId)
                .OrderByDescending(item => item.CreatedAt)
                .ToList());
    }

    public string ComputeContentHash(Guid versionId)
    {
        var snapshot = GetSnapshot(versionId);
        var canonical = new
        {
            Procedure = new { snapshot.Procedure.ProcedureCode, snapshot.Procedure.Name, snapshot.Procedure.ProcedureType, snapshot.Procedure.OwnerDepartmentId, snapshot.Procedure.Description },
            Version = new
            {
                snapshot.Version.VersionNo,
                snapshot.Version.VersionLabel,
                snapshot.Version.Title,
                Summary = NormalizeHashText(snapshot.Version.Summary),
                snapshot.Version.ChangeReason,
                snapshot.Version.DepartmentId,
                IssueDate = ToHashDate(snapshot.Version.IssueDate),
                snapshot.Version.IssueNumber,
                snapshot.Version.SourcePdfFileName,
                snapshot.Version.SourcePdfChecksumSha256,
                snapshot.Version.RequiredWriterSignatures
            },
            Writers = snapshot.WriterAssignments.Select(item => new { item.DisplayOrder, item.AssignedUserId, item.AssignedUsername, item.AssignedFullName, item.SignoffRole }),
            Sections = snapshot.Sections.Select(s => new { s.SectionOrder, s.SectionNumber, s.Title, s.SectionKind, ContentText = NormalizeHashText(s.ContentText), s.IsRequired }),
            Recipients = snapshot.Recipients.Select(r => new { r.DisplayOrder, r.RecipientName, r.IsMarked }),
            Revisions = snapshot.Revisions.Select(r => new { r.DisplayOrder, RevisionDate = ToHashDate(r.RevisionDate), r.PageRef, r.SectionRef, r.Summary }),
            Steps = snapshot.Steps.Select(s => new { s.StepNo, s.StepCode, s.Name, Description = NormalizeHashText(s.Description), ResponsibilityText = NormalizeHashText(s.ResponsibilityText), s.FlowShapeCode, FormReferenceText = NormalizeHashText(s.FormReferenceText), s.FormAttachmentId, s.DetailSectionNumber, s.StandardDurationMinutes, s.IsRequired }),
            StepRoles = snapshot.StepRoleAssignments.Select(item => new { item.ProcedureStepId, item.RoleId, item.DisplayOrder }),
            StepLocations = snapshot.StepLocationAssignments.Select(item => new { item.ProcedureStepId, item.DepartmentId, item.DisplayOrder }),
            StepAttachments = snapshot.StepAttachmentAssignments.Select(item => new { item.ProcedureStepId, item.ProcedureAttachmentId, item.DisplayOrder }),
            Attachments = snapshot.Attachments.Select(a => new { a.AttachmentType, a.FileName, a.FileUri, a.MimeType, a.FileSizeBytes, a.ChecksumSha256 })
        };
        var json = JsonSerializer.Serialize(canonical, HashJsonOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }

    private static readonly JsonSerializerOptions HashJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private static string? NormalizeHashText(string? value)
        => string.IsNullOrWhiteSpace(value) ? value : value.Trim();

    private static string? ToHashDate(DateTime? value)
        => value.HasValue ? DateOnly.FromDateTime(value.Value).ToString("yyyy-MM-dd") : null;

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
        if (snapshot.WriterAssignments.Count < Math.Max(1, snapshot.Version.RequiredWriterSignatures))
            missing.Add($"Phân công đủ {Math.Max(1, snapshot.Version.RequiredWriterSignatures)} người viết");

        foreach (var kind in RequiredSectionKinds)
        {
            var section = snapshot.Sections.FirstOrDefault(s => string.Equals(s.SectionKind, kind, StringComparison.OrdinalIgnoreCase));
            if (section is null || (section.IsRequired && string.IsNullOrWhiteSpace(section.ContentText)))
            {
                missing.Add($"Mục {SectionKindLabel(kind)}");
            }
        }

        if (ContainsOcrPending(snapshot.Version.Summary) ||
            snapshot.Sections.Any(s => ContainsOcrPending(s.ContentText)) ||
            snapshot.Steps.Any(s => ContainsOcrPending(s.Description) || ContainsOcrPending(s.FormReferenceText)))
        {
            missing.Add("OCR đầy đủ từng trang PDF");
        }

        if (requireSignoffs)
        {
            foreach (var role in ProcedureSignoffService.RequiredRoles)
            {
                if (!HasCurrentSignoff(snapshot, role))
                    missing.Add($"Chữ ký {SignoffRoleLabel(role)}");
            }
        }

        return new ProcedureDocumentReadiness(missing.Count == 0, missing);
    }

    private static string SectionKindLabel(string kind) => kind switch
    {
        "purpose" => "Mục đích",
        "scope" => "Phạm vi áp dụng",
        "basis" => "Căn cứ và tài liệu viện dẫn",
        "definitions" => "Thuật ngữ và định nghĩa",
        "responsibilities" => "Trách nhiệm",
        "procedure" => "Nội dung quy trình",
        "flowchart" => "Lưu đồ",
        "records" => "Hồ sơ và biểu mẫu",
        "appendices" => "Phụ lục",
        _ => kind
    };

    private static string SignoffRoleLabel(string role) => role switch
    {
        "writer" => "Người viết",
        "checker" => "Người kiểm tra",
        "approver" => "Người phê duyệt",
        _ => role
    };

    private static bool ContainsOcrPending(string? value)
        => value?.Contains("OCR_PENDING", StringComparison.OrdinalIgnoreCase) == true;

    public bool HasCurrentSignoff(ProcedureDocumentSnapshot snapshot, string role)
    {
        var hash = ComputeContentHash(snapshot.Version.ProcedureVersionId);
        var currentCount = snapshot.Signoffs.Count(s =>
            string.Equals(s.SignoffRole, role, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(s.ContentHashSha256, hash, StringComparison.OrdinalIgnoreCase));
        return currentCount >= RequiredSignoffCount(snapshot, role);
    }

    public int RequiredSignoffCount(ProcedureDocumentSnapshot snapshot, string role)
        => string.Equals(role, "writer", StringComparison.OrdinalIgnoreCase)
            ? Math.Max(1, snapshot.Version.RequiredWriterSignatures)
            : 1;

    public IReadOnlyList<ProcedureSignoffRecord> GetCurrentSignoffs(ProcedureDocumentSnapshot snapshot, string role)
    {
        var hash = ComputeContentHash(snapshot.Version.ProcedureVersionId);
        return GetSignoffsForHash(snapshot, role, hash);
    }

    /// <summary>
    /// Chữ ký hiển thị trên lịch sử/chi tiết: với bản đã gửi duyệt hoặc ban hành dùng hash snapshot vòng đời;
    /// với bản nháp vẫn dùng hash nội dung hiện tại.
    /// </summary>
    public IReadOnlyList<ProcedureSignoffRecord> GetDisplaySignoffs(ProcedureDocumentSnapshot snapshot, string role)
    {
        var hash = GetAuthoritativeContentHash(snapshot);
        var matched = GetSignoffsForHash(snapshot, role, hash);
        if (matched.Count > 0)
            return matched;

        if (string.Equals(role, "writer", StringComparison.OrdinalIgnoreCase))
        {
            return snapshot.Signoffs
                .Where(s => string.Equals(s.SignoffRole, "writer", StringComparison.OrdinalIgnoreCase) && s.SignerUserId.HasValue)
                .GroupBy(s => s.SignerUserId!.Value)
                .Select(group => group.OrderByDescending(item => item.SignedAt).First())
                .OrderBy(s => s.DisplayOrder)
                .ThenBy(s => s.SignedAt)
                .ToList();
        }

        return snapshot.Signoffs
            .Where(s => string.Equals(s.SignoffRole, role, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(s => s.SignedAt)
            .Take(1)
            .ToList();
    }

    public ProcedureSignoffRecord? GetWriterSignoffForDisplay(ProcedureDocumentSnapshot snapshot, Guid assignedUserId)
    {
        var display = GetDisplaySignoffs(snapshot, "writer")
            .FirstOrDefault(item => item.SignerUserId == assignedUserId);
        if (display is not null)
            return display;

        return snapshot.Signoffs
            .Where(item =>
                string.Equals(item.SignoffRole, "writer", StringComparison.OrdinalIgnoreCase) &&
                item.SignerUserId == assignedUserId)
            .OrderByDescending(item => item.SignedAt)
            .FirstOrDefault();
    }

    public bool IsSignoffStale(ProcedureDocumentSnapshot snapshot, ProcedureSignoffRecord signoff)
    {
        if (!string.Equals(snapshot.Version.StatusCode, "draft", StringComparison.OrdinalIgnoreCase))
            return false;

        var currentHash = ComputeContentHash(snapshot.Version.ProcedureVersionId);
        return !string.Equals(signoff.ContentHashSha256, currentHash, StringComparison.OrdinalIgnoreCase);
    }

    public string GetAuthoritativeContentHash(ProcedureDocumentSnapshot snapshot)
    {
        if (string.Equals(snapshot.Version.StatusCode, "draft", StringComparison.OrdinalIgnoreCase))
            return ComputeContentHash(snapshot.Version.ProcedureVersionId);

        var lifecycleSnapshot = snapshot.VersionSnapshots
            .OrderByDescending(item => item.SnapshotKind switch
            {
                "published" => 4,
                "submitted" => 3,
                "draft_signed" => 2,
                _ => 1
            })
            .ThenByDescending(item => item.CreatedAt)
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(lifecycleSnapshot?.ContentHashSha256))
            return lifecycleSnapshot.ContentHashSha256;

        var latestWriterHash = snapshot.Signoffs
            .Where(item => string.Equals(item.SignoffRole, "writer", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.SignedAt)
            .Select(item => item.ContentHashSha256)
            .FirstOrDefault(hash => !string.IsNullOrWhiteSpace(hash));

        return !string.IsNullOrWhiteSpace(latestWriterHash)
            ? latestWriterHash
            : ComputeContentHash(snapshot.Version.ProcedureVersionId);
    }

    private static IReadOnlyList<ProcedureSignoffRecord> GetSignoffsForHash(
        ProcedureDocumentSnapshot snapshot,
        string role,
        string hash)
        => snapshot.Signoffs
            .Where(s =>
                string.Equals(s.SignoffRole, role, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(s.ContentHashSha256, hash, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.SignedAt)
            .ToList();

    public ProcedureVersionSnapshotRecord PersistSnapshot(Guid versionId, string snapshotKind, Guid? createdBy = null)
    {
        var snapshot = GetSnapshot(versionId);
        var json = SerializeSnapshot(snapshot);
        var record = new ProcedureVersionSnapshotRecord
        {
            ProcedureVersionId = versionId,
            SnapshotKind = string.IsNullOrWhiteSpace(snapshotKind) ? "draft" : snapshotKind.Trim(),
            ContentHashSha256 = ComputeContentHash(versionId),
            SnapshotJson = json,
            CreatedBy = createdBy
        };
        _store.AddProcedureVersionSnapshot(record);
        return record;
    }

    public ProcedureVersionDiffRecord? PersistVersionDiff(Guid? sourceVersionId, Guid targetVersionId, Guid? createdBy = null)
    {
        if (sourceVersionId is null) return null;
        var source = GetSnapshot(sourceVersionId.Value);
        var target = GetSnapshot(targetVersionId);
        var diff = new ProcedureVersionDiffRecord
        {
            ProcedureId = target.Procedure.ProcedureId,
            FromVersionId = source.Version.ProcedureVersionId,
            ToVersionId = target.Version.ProcedureVersionId,
            DiffJson = BuildDiffJson(source, target),
            CreatedBy = createdBy
        };
        _store.AddOrUpdateProcedureVersionDiff(diff);
        return diff;
    }

    private static string SerializeSnapshot(ProcedureDocumentSnapshot snapshot)
    {
        var payload = new
        {
            procedure = new
            {
                snapshot.Procedure.ProcedureId,
                snapshot.Procedure.ProcedureCode,
                snapshot.Procedure.Name,
                snapshot.Procedure.ProcedureType,
                snapshot.Procedure.OwnerDepartmentId,
                snapshot.Procedure.Description
            },
            version = new
            {
                snapshot.Version.ProcedureVersionId,
                snapshot.Version.VersionNo,
                snapshot.Version.VersionLabel,
                snapshot.Version.Title,
                snapshot.Version.Summary,
                snapshot.Version.ChangeReason,
                snapshot.Version.DepartmentId,
                snapshot.Version.IssueDate,
                snapshot.Version.IssueNumber,
                snapshot.Version.SourcePdfFileName,
                snapshot.Version.SourcePdfChecksumSha256,
                snapshot.Version.RequiredWriterSignatures
            },
            writers = snapshot.WriterAssignments,
            sections = snapshot.Sections,
            recipients = snapshot.Recipients,
            revisions = snapshot.Revisions,
            steps = snapshot.Steps,
            stepRoles = snapshot.StepRoleAssignments,
            stepLocations = snapshot.StepLocationAssignments,
            stepAttachments = snapshot.StepAttachmentAssignments,
            attachments = snapshot.Attachments,
            signoffs = snapshot.Signoffs
        };
        return JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private static string BuildDiffJson(ProcedureDocumentSnapshot source, ProcedureDocumentSnapshot target)
    {
        var diff = new JsonObject
        {
            ["fromVersionLabel"] = source.Version.VersionLabel ?? $"v{source.Version.VersionNo:00}",
            ["toVersionLabel"] = target.Version.VersionLabel ?? $"v{target.Version.VersionNo:00}",
            ["metadata"] = new JsonArray(BuildTextChange("Tiêu đề", source.Version.Title, target.Version.Title),
                BuildTextChange("Tóm tắt", source.Version.Summary, target.Version.Summary),
                BuildTextChange("Lý do thay đổi", source.Version.ChangeReason, target.Version.ChangeReason),
                BuildTextChange("Lần ban hành", source.Version.IssueNumber?.ToString(), target.Version.IssueNumber?.ToString())),
            ["writers"] = BuildListChange(
                source.WriterAssignments.Select(item => item.AssignedFullName ?? item.AssignedUsername ?? item.AssignedUserId.ToString()),
                target.WriterAssignments.Select(item => item.AssignedFullName ?? item.AssignedUsername ?? item.AssignedUserId.ToString())),
            ["sections"] = BuildListChange(
                source.Sections.Select(item => $"{item.SectionNumber}|{item.Title}|{item.ContentText}"),
                target.Sections.Select(item => $"{item.SectionNumber}|{item.Title}|{item.ContentText}")),
            ["steps"] = BuildListChange(
                source.Steps.Select(item => $"{item.StepNo}|{item.Name}|{item.ResponsibilityText}|{item.Description}"),
                target.Steps.Select(item => $"{item.StepNo}|{item.Name}|{item.ResponsibilityText}|{item.Description}")),
            ["attachments"] = BuildListChange(
                source.Attachments.Select(item => $"{item.AttachmentType}|{item.FileName}"),
                target.Attachments.Select(item => $"{item.AttachmentType}|{item.FileName}"))
        };
        return diff.ToJsonString();
    }

    private static JsonObject BuildTextChange(string label, string? before, string? after)
    {
        var normalizedBefore = FormatDiffField(label, before);
        var normalizedAfter = FormatDiffField(label, after);
        return new JsonObject
        {
            ["label"] = label,
            ["before"] = normalizedBefore,
            ["after"] = normalizedAfter,
            ["changed"] = !string.Equals(normalizedBefore, normalizedAfter, StringComparison.Ordinal)
        };
    }

    private static string FormatDiffField(string label, string? value)
    {
        if (string.Equals(label, "Tóm tắt", StringComparison.Ordinal))
            return ProcedureVersionSummaryFormatter.Display(value);

        return ProcedureVersionSummaryFormatter.NormalizeDisplayText(value);
    }

    private static JsonObject BuildListChange(IEnumerable<string> before, IEnumerable<string> after)
    {
        var beforeList = before.Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.Ordinal).OrderBy(item => item).ToList();
        var afterList = after.Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.Ordinal).OrderBy(item => item).ToList();
        return new JsonObject
        {
            ["before"] = new JsonArray(beforeList.Select(item => JsonValue.Create(item)).ToArray()),
            ["after"] = new JsonArray(afterList.Select(item => JsonValue.Create(item)).ToArray()),
            ["added"] = new JsonArray(afterList.Except(beforeList, StringComparer.Ordinal).Select(item => JsonValue.Create(item)).ToArray()),
            ["removed"] = new JsonArray(beforeList.Except(afterList, StringComparer.Ordinal).Select(item => JsonValue.Create(item)).ToArray())
        };
    }
}

public sealed record ProcedureDocumentSnapshot(
    ProfessionalProcedure Procedure,
    ProcedureVersion Version,
    string? DepartmentName,
    IReadOnlyList<ProcedureVersionAuthorAssignment> WriterAssignments,
    IReadOnlyList<ProcedureDocumentSection> Sections,
    IReadOnlyList<ProcedureDistributionRecipient> Recipients,
    IReadOnlyList<ProcedureRevisionEntry> Revisions,
    IReadOnlyList<ProcedureStep> Steps,
    IReadOnlyList<ProcedureStepRoleAssignment> StepRoleAssignments,
    IReadOnlyList<ProcedureStepLocationAssignment> StepLocationAssignments,
    IReadOnlyList<ProcedureStepAttachmentAssignment> StepAttachmentAssignments,
    IReadOnlyList<ProcedureAttachment> Attachments,
    IReadOnlyList<ProcedureSignoffRecord> Signoffs,
    IReadOnlyList<ProcedureVersionSnapshotRecord> VersionSnapshots);

public sealed record ProcedureDocumentReadiness(bool IsReady, IReadOnlyList<string> MissingItems);
