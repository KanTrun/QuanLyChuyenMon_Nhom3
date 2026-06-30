using System.Text.Json;
using TelemedicineLandingPage.Models.Admin;
using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin;

public sealed class ProcedureAuthoringService
{
    private readonly IMedDataStore _store;
    private readonly ProcedureDocumentSnapshotService? _snapshots;

    public ProcedureAuthoringService(IMedDataStore store)
    {
        _store = store;
    }

    public ProcedureAuthoringService(IMedDataStore store, ProcedureDocumentSnapshotService snapshots)
    {
        _store = store;
        _snapshots = snapshots;
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
            CreatedBy = command.UserId,
            RequiredWriterSignatures = Math.Max(1, command.WriterAssignments
                .Select(item => ParseGuid(item.UserId))
                .Where(item => item.HasValue)
                .Select(item => item!.Value)
                .Distinct()
                .Count())
        };

        _store.AddProcedureVersion(version);
        PersistDocument(command, version);
        PersistWriterAssignments(command, version);
        CloneTechnicalMappings(source, version);
        ArchiveSourceDraft(source, versionLabel);
        if (command.ProcedureId.HasValue) _store.UpdateProcedure(procedure);
        _snapshots?.PersistSnapshot(version.ProcedureVersionId, "draft", command.UserId);
        _snapshots?.PersistVersionDiff(source?.ProcedureVersionId, version.ProcedureVersionId, command.UserId);
        return new ProcedureAuthoringResult(procedure, version);
    }

    public ProcedureAuthoringResult UpdateDraft(ProcedureAuthoringCommand command)
    {
        if (command.ProcedureId is not { } procedureId)
            throw new InvalidOperationException("Cập nhật bản nháp phải gắn với một quy trình hiện có.");

        var version = _store.ProcedureVersions.FirstOrDefault(item => item.ProcedureVersionId == command.VersionId)
            ?? throw new InvalidOperationException("Không tìm thấy phiên bản nháp cần cập nhật.");
        if (version.ProcedureId != procedureId)
            throw new InvalidOperationException("Phiên bản nháp không thuộc quy trình đang chỉnh sửa.");
        if (!string.Equals(version.StatusCode, "draft", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Chỉ có thể chỉnh sửa phiên bản ở trạng thái bản nháp.");

        var procedure = _store.Procedures.First(item => item.ProcedureId == procedureId);
        var sourcePdf = command.Attachments.LastOrDefault(item => item.AttachmentType == "source_pdf");
        var requiredWriters = Math.Max(1, command.WriterAssignments
            .Select(item => ParseGuid(item.UserId))
            .Where(item => item.HasValue)
            .Select(item => item!.Value)
            .Distinct()
            .Count());
        var updatedProcedure = procedure with
        {
            Name = command.Name.Trim(),
            ProcedureType = command.ProcedureType,
            OwnerDepartmentId = command.DepartmentId,
            Description = command.Description
        };
        var updatedVersion = version with
        {
            DepartmentId = command.DepartmentId,
            Title = $"{updatedProcedure.Name} - {version.VersionLabel ?? FormatVersionLabel(version.VersionNo)}",
            Summary = JsonSerializer.Serialize(new { note = command.SummaryText ?? "Cập nhật bản nháp quy trình" }),
            IssueDate = command.IssueDate,
            IssueNumber = command.IssueNumber,
            SourcePdfFileName = sourcePdf?.FileName ?? version.SourcePdfFileName,
            SourcePdfChecksumSha256 = sourcePdf?.ChecksumSha256 ?? version.SourcePdfChecksumSha256,
            RequiredWriterSignatures = requiredWriters
        };

        _store.UpdateProcedure(updatedProcedure);
        _store.UpdateProcedureVersion(updatedVersion);
        _store.ClearProcedureVersionDocument(version.ProcedureVersionId);
        PersistDocument(command, updatedVersion);
        PersistWriterAssignments(command, updatedVersion);
        _snapshots?.PersistSnapshot(updatedVersion.ProcedureVersionId, "draft", command.UserId);
        return new ProcedureAuthoringResult(updatedProcedure, updatedVersion);
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

        var attachmentIdByClient = new Dictionary<Guid, Guid>();
        foreach (var attachment in command.Attachments)
        {
            var persisted = new ProcedureAttachment
            {
                ProcedureVersionId = version.ProcedureVersionId,
                AttachmentType = attachment.AttachmentType,
                FileName = attachment.FileName,
                FileUri = attachment.FileUri,
                MimeType = attachment.MimeType,
                FileSizeBytes = attachment.FileSizeBytes,
                ChecksumSha256 = attachment.ChecksumSha256,
                UploadedBy = command.UserId
            };
            _store.AddProcedureAttachment(persisted);
            attachmentIdByClient[attachment.ClientId] = persisted.ProcedureAttachmentId;
        }

        foreach (var item in command.Steps.Where(item => !string.IsNullOrWhiteSpace(item.Name)).Select((value, index) => (value, index)))
        {
            Guid? formAttachmentId = null;
            var linkedAttachmentIds = item.value.LinkedAttachmentClientIds.ToList();
            if (linkedAttachmentIds.Count == 0 && item.value.LinkedAttachmentClientId is { } singleClientId)
            {
                linkedAttachmentIds.Add(singleClientId);
            }
            if (linkedAttachmentIds.Count > 0 &&
                attachmentIdByClient.TryGetValue(linkedAttachmentIds[0], out var resolvedId))
            {
                formAttachmentId = resolvedId;
            }

            var step = new Models.Admin.Sql.ProcedureStep
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
                FormAttachmentId = formAttachmentId,
                DetailSectionNumber = NullIfWhiteSpace(item.value.DetailSectionNumber),
                StandardDurationMinutes = item.value.Minutes
            };
            _store.AddProcedureStep(step);

            var roleIds = item.value.RoleIds
                .Append(item.value.RoleId)
                .Select(ParseGuid)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();
            foreach (var role in roleIds.Select((value, roleIndex) => (value, roleIndex)))
            {
                _store.AddProcedureStepRoleAssignment(new ProcedureStepRoleAssignment
                {
                    ProcedureStepId = step.ProcedureStepId,
                    RoleId = role.value,
                    DisplayOrder = role.roleIndex + 1
                });
            }

            var locationIds = item.value.LocationDepartmentIds
                .Select(ParseGuid)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();
            foreach (var location in locationIds.Select((value, locationIndex) => (value, locationIndex)))
            {
                _store.AddProcedureStepLocationAssignment(new ProcedureStepLocationAssignment
                {
                    ProcedureStepId = step.ProcedureStepId,
                    DepartmentId = location.value,
                    DisplayOrder = location.locationIndex + 1
                });
            }

            foreach (var attachment in linkedAttachmentIds
                         .Distinct()
                         .Select((value, attachmentIndex) => (value, attachmentIndex)))
            {
                if (!attachmentIdByClient.TryGetValue(attachment.value, out var persistedAttachmentId))
                    continue;
                _store.AddProcedureStepAttachmentAssignment(new ProcedureStepAttachmentAssignment
                {
                    ProcedureStepId = step.ProcedureStepId,
                    ProcedureAttachmentId = persistedAttachmentId,
                    DisplayOrder = attachment.attachmentIndex + 1
                });
            }
        }
    }

    private void PersistWriterAssignments(ProcedureAuthoringCommand command, ProcedureVersion version)
    {
        var writers = command.WriterAssignments
            .Select(item => ParseGuid(item.UserId))
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
        if (writers.Count == 0)
        {
            writers.Add(command.UserId);
        }

        foreach (var writer in writers.Select((value, index) => (value, index)))
        {
            var user = _store.Users.FirstOrDefault(item => item.UserId == writer.value);
            _store.AddProcedureVersionAuthorAssignment(new ProcedureVersionAuthorAssignment
            {
                ProcedureVersionId = version.ProcedureVersionId,
                SignoffRole = "writer",
                DisplayOrder = writer.index + 1,
                AssignedUserId = writer.value,
                AssignedUsername = user?.Username,
                AssignedFullName = user?.FullName
            });
        }
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
