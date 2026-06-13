using System.Text.Json;
using TelemedicineLandingPage.Models.Admin;
using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin;

public sealed class ProcedureAuthoringService
{
    private readonly IMedDataStore _store;

    public ProcedureAuthoringService(IMedDataStore store)
    {
        _store = store;
    }

    public static string FormatVersionLabel(int versionNo) => $"v{versionNo:00}";

    public int GetNextVersionNo(Guid procedureId)
        => _store.ProcedureVersions
            .Where(item => item.ProcedureId == procedureId)
            .Select(item => item.VersionNo)
            .DefaultIfEmpty(0)
            .Max() + 1;

    public ProcedureAuthoringResult CreateVersion(ProcedureAuthoringCommand command)
    {
        var source = GetSourceVersion(command);
        var procedure = GetOrCreateProcedure(command);
        var versionNo = command.ProcedureId.HasValue ? GetNextVersionNo(procedure.ProcedureId) : 1;
        var versionLabel = FormatVersionLabel(versionNo);
        var sourcePdf = command.Attachments.LastOrDefault(item => item.AttachmentType == "source_pdf");
        var version = new ProcedureVersion
        {
            ProcedureVersionId = command.VersionId,
            ProcedureId = procedure.ProcedureId,
            VersionNo = versionNo,
            VersionLabel = versionLabel,
            StatusCode = "draft",
            DepartmentId = command.DepartmentId,
            Title = $"{procedure.Name} - {versionLabel}",
            Summary = JsonSerializer.Serialize(new { note = command.SummaryText ?? "Khởi tạo quy trình" }),
            ChangeReason = source is null ? "Khởi tạo phiên bản đầu tiên" : $"Cập nhật từ {source.VersionLabel ?? FormatVersionLabel(source.VersionNo)}",
            IssueDate = command.IssueDate,
            IssueNumber = command.IssueNumber,
            SourcePdfFileName = sourcePdf?.FileName,
            SourcePdfChecksumSha256 = sourcePdf?.ChecksumSha256,
            CreatedBy = command.UserId
        };

        _store.AddProcedureVersion(version);
        PersistDocument(command, version);
        CloneTechnicalMappings(source, version);
        ArchiveSourceDraft(source, versionLabel);
        if (command.ProcedureId.HasValue) _store.UpdateProcedure(procedure);
        return new ProcedureAuthoringResult(procedure, version);
    }

    private ProfessionalProcedure GetOrCreateProcedure(ProcedureAuthoringCommand command)
    {
        if (command.ProcedureId is { } procedureId)
        {
            return _store.Procedures.First(item => item.ProcedureId == procedureId);
        }

        var procedure = new ProfessionalProcedure
        {
            ProcedureCode = command.Code.Trim(),
            Name = command.Name.Trim(),
            ProcedureType = command.ProcedureType,
            OwnerDepartmentId = command.DepartmentId,
            Description = command.Description,
            CreatedBy = command.UserId
        };
        _store.AddProcedure(procedure);
        return procedure;
    }

    private ProcedureVersion? GetSourceVersion(ProcedureAuthoringCommand command)
    {
        if (command.SourceVersionId is not { } sourceVersionId) return null;
        var source = _store.ProcedureVersions.First(item => item.ProcedureVersionId == sourceVersionId);
        if (source.ProcedureId != command.ProcedureId)
            throw new InvalidOperationException("Phiên bản nguồn không thuộc quy trình đang cập nhật.");
        if (source.StatusCode == "pending_approval")
            throw new InvalidOperationException("Phiên bản đang chờ phê duyệt. Hãy hoàn tất hoặc từ chối trước khi tạo bản mới.");
        return source;
    }

    private void PersistDocument(ProcedureAuthoringCommand command, ProcedureVersion version)
    {
        foreach (var section in command.Sections)
            _store.AddProcedureDocumentSection(new ProcedureDocumentSection
            {
                ProcedureVersionId = version.ProcedureVersionId,
                SectionOrder = section.Order,
                SectionNumber = section.Number,
                Title = section.Title,
                SectionKind = section.Kind,
                ContentText = NullIfWhiteSpace(section.Content),
                IsRequired = section.IsRequired
            });

        foreach (var item in command.Recipients.Where(item => !string.IsNullOrWhiteSpace(item.Name)).Select((value, index) => (value, index)))
            _store.AddProcedureDistributionRecipient(new ProcedureDistributionRecipient
            {
                ProcedureVersionId = version.ProcedureVersionId,
                DisplayOrder = item.index + 1,
                RecipientName = item.value.Name.Trim(),
                IsMarked = item.value.IsMarked
            });

        foreach (var item in command.Revisions.Where(item => !string.IsNullOrWhiteSpace(item.Summary)).Select((value, index) => (value, index)))
            _store.AddProcedureRevisionEntry(new ProcedureRevisionEntry
            {
                ProcedureVersionId = version.ProcedureVersionId,
                DisplayOrder = item.index + 1,
                RevisionDate = item.value.RevisionDate,
                PageRef = NullIfWhiteSpace(item.value.PageReference),
                SectionRef = NullIfWhiteSpace(item.value.SectionReference),
                Summary = item.value.Summary.Trim()
            });

        foreach (var item in command.Steps.Where(item => !string.IsNullOrWhiteSpace(item.Name)).Select((value, index) => (value, index)))
            _store.AddProcedureStep(new Models.Admin.Sql.ProcedureStep
            {
                ProcedureVersionId = version.ProcedureVersionId,
                StepNo = item.index + 1,
                StepCode = NullIfWhiteSpace(item.value.Code) ?? $"BUOC-{item.index + 1:00}",
                Name = item.value.Name.Trim(),
                Description = NullIfWhiteSpace(item.value.Description),
                ActorRoleId = ParseGuid(item.value.RoleId),
                ResponsibilityText = NullIfWhiteSpace(item.value.Responsibility),
                FlowShapeCode = item.value.ShapeCode,
                FormReferenceText = NullIfWhiteSpace(item.value.FormReference),
                DetailSectionNumber = NullIfWhiteSpace(item.value.DetailSectionNumber),
                StandardDurationMinutes = item.value.Minutes
            });

        foreach (var attachment in command.Attachments)
            _store.AddProcedureAttachment(new ProcedureAttachment
            {
                ProcedureVersionId = version.ProcedureVersionId,
                AttachmentType = attachment.AttachmentType,
                FileName = attachment.FileName,
                FileUri = attachment.FileUri,
                MimeType = attachment.MimeType,
                FileSizeBytes = attachment.FileSizeBytes,
                ChecksumSha256 = attachment.ChecksumSha256,
                UploadedBy = command.UserId
            });
    }

    private void ArchiveSourceDraft(ProcedureVersion? source, string nextVersionLabel)
    {
        if (source?.StatusCode is not ("draft" or "rejected")) return;
        _store.UpdateProcedureVersion(source with
        {
            StatusCode = "archived",
            EffectiveTo = DateTime.UtcNow,
            ChangeReason = $"Lưu trữ khi tạo {nextVersionLabel}"
        });
    }

    private void CloneTechnicalMappings(ProcedureVersion? source, ProcedureVersion target)
    {
        if (source is null) return;
        var resourceNorms = _store.ProcedureVersionResourceNorms
            .Where(item => item.ProcedureVersionId == source.ProcedureVersionId)
            .ToList();
        foreach (var norm in resourceNorms)
            _store.AddProcedureVersionResourceNorm(norm with
            {
                ProcedureVersionResourceNormId = Guid.NewGuid(),
                ProcedureVersionId = target.ProcedureVersionId,
                CreatedAt = DateTime.UtcNow
            });

        var screenMappings = _store.ProcedureScreenMappings
            .Where(item => item.ProcedureVersionId == source.ProcedureVersionId)
            .ToList();
        foreach (var mapping in screenMappings)
            _store.AddProcedureScreenMapping(mapping with
            {
                ProcedureScreenMappingId = Guid.NewGuid(),
                ProcedureVersionId = target.ProcedureVersionId,
                CreatedAt = DateTime.UtcNow
            });
    }

    private static Guid? ParseGuid(string value) => Guid.TryParse(value, out var id) ? id : null;
    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
