using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>Phần quản lý quy trình, phiên bản, bước, đính kèm, ánh xạ màn hình.</summary>
public sealed partial class MedDataStore
{
    public void AddProcedure(ProfessionalProcedure proc)
    {
        lock (_lock)
        {
            if (_procedures.Any(p => p.ProcedureCode == proc.ProcedureCode))
                throw MedDomainException.Constraint("UQ_procedures_code", 2627, $"Mã quy trình '{proc.ProcedureCode}' đã tồn tại.");
            _procedures.Add(proc);
            RaiseStateChanged();
        }
    }

    public void UpdateProcedure(ProfessionalProcedure proc)
    {
        lock (_lock)
        {
            var idx = _procedures.FindIndex(p => p.ProcedureId == proc.ProcedureId);
            if (idx < 0)
                throw MedDomainException.Constraint("PK_procedures", 547, "Quy trình không tồn tại.");

            if (_procedures.Any(p => p.ProcedureId != proc.ProcedureId && p.ProcedureCode == proc.ProcedureCode))
                throw MedDomainException.Constraint("UQ_procedures_code", 2627, $"Mã quy trình '{proc.ProcedureCode}' đã tồn tại.");

            var current = _procedures[idx];
            _procedures[idx] = proc with
            {
                CreatedAt = current.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };
            RaiseStateChanged();
        }
    }

    public void AddProcedureVersion(ProcedureVersion ver)
    {
        lock (_lock)
        {
            ValidateJson(ver.Summary, "summary");
            _procedureVersions.Add(ver);
            RaiseStateChanged();
        }
    }

    public void UpdateProcedureVersion(ProcedureVersion updated)
    {
        lock (_lock)
        {
            var idx = _procedureVersions.FindIndex(v => v.ProcedureVersionId == updated.ProcedureVersionId);
            if (idx < 0)
                throw MedDomainException.Constraint("FK_procedure_version", 547, "Phiên bản quy trình không tồn tại.");
            _procedureVersions[idx] = updated;
            RaiseStateChanged();
        }
    }

    public void AddProcedureStep(ProcedureStep step)
    {
        lock (_lock)
        {
            ValidateJson(step.TransitionConditionJson, "transition_condition");
            _procedureSteps.Add(step);
            RaiseStateChanged();
        }
    }

    public void AddProcedureAttachment(ProcedureAttachment att)
    {
        lock (_lock)
        {
            _procedureAttachments.Add(att);
            RaiseStateChanged();
        }
    }

    public void RemoveProcedureAttachment(Guid attachmentId)
    {
        lock (_lock)
        {
            var removed = _procedureAttachments.RemoveAll(a => a.ProcedureAttachmentId == attachmentId);
            if (removed == 0)
                throw MedDomainException.Constraint("PK_procedure_attachments", 547, "Tệp đính kèm không tồn tại.");
            RaiseStateChanged();
        }
    }

    public void AddProcedureScreenMapping(ProcedureScreenMapping mapping)
    {
        lock (_lock)
        {
            ValidateJson(mapping.RuleJson, "rule");
            _procedureScreenMappings.Add(mapping);
            RaiseStateChanged();
        }
    }

    public void RemoveProcedureScreenMapping(Guid mappingId)
    {
        lock (_lock)
        {
            var removed = _procedureScreenMappings.RemoveAll(m => m.ProcedureScreenMappingId == mappingId);
            if (removed == 0)
                throw MedDomainException.Constraint("PK_procedure_screen_mappings", 547, "Ánh xạ màn hình không tồn tại.");
            RaiseStateChanged();
        }
    }

    public void AddProcedureDocumentSection(ProcedureDocumentSection section)
    {
        lock (_lock)
        {
            _procedureDocumentSections.Add(section);
            RaiseStateChanged();
        }
    }

    public void UpdateProcedureDocumentSection(ProcedureDocumentSection section)
    {
        lock (_lock)
        {
            var idx = _procedureDocumentSections.FindIndex(s => s.ProcedureDocumentSectionId == section.ProcedureDocumentSectionId);
            if (idx < 0)
                throw MedDomainException.Constraint("PK_procedure_document_sections", 547, "Muc tai lieu quy trinh khong ton tai.");
            _procedureDocumentSections[idx] = section;
            RaiseStateChanged();
        }
    }

    public void AddProcedureDistributionRecipient(ProcedureDistributionRecipient recipient)
    {
        lock (_lock)
        {
            _procedureDistributionRecipients.Add(recipient);
            RaiseStateChanged();
        }
    }

    public void AddProcedureRevisionEntry(ProcedureRevisionEntry revision)
    {
        lock (_lock)
        {
            _procedureRevisionEntries.Add(revision);
            RaiseStateChanged();
        }
    }

    public void AddProcedureSignoffRecord(ProcedureSignoffRecord signoff)
    {
        lock (_lock)
        {
            _procedureSignoffRecords.Add(signoff);
            RaiseStateChanged();
        }
    }

    public void AddProcedureVersionAuthorAssignment(ProcedureVersionAuthorAssignment assignment)
    {
        lock (_lock)
        {
            _procedureVersionAuthorAssignments.Add(assignment);
            RaiseStateChanged();
        }
    }

    public void ClearProcedureVersionDocument(Guid versionId)
    {
        lock (_lock)
        {
            var stepIds = _procedureSteps
                .Where(item => item.ProcedureVersionId == versionId)
                .Select(item => item.ProcedureStepId)
                .ToHashSet();
            _procedureStepAttachmentAssignments.RemoveAll(item => stepIds.Contains(item.ProcedureStepId));
            _procedureStepRoleAssignments.RemoveAll(item => stepIds.Contains(item.ProcedureStepId));
            _procedureStepLocationAssignments.RemoveAll(item => stepIds.Contains(item.ProcedureStepId));
            _procedureSteps.RemoveAll(item => item.ProcedureVersionId == versionId);
            _procedureDocumentSections.RemoveAll(item => item.ProcedureVersionId == versionId);
            _procedureDistributionRecipients.RemoveAll(item => item.ProcedureVersionId == versionId);
            _procedureRevisionEntries.RemoveAll(item => item.ProcedureVersionId == versionId);
            _procedureVersionAuthorAssignments.RemoveAll(item => item.ProcedureVersionId == versionId);
            _procedureAttachments.RemoveAll(item => item.ProcedureVersionId == versionId);
            RaiseStateChanged();
        }
    }

    public void AddProcedureStepRoleAssignment(ProcedureStepRoleAssignment assignment)
    {
        lock (_lock)
        {
            _procedureStepRoleAssignments.Add(assignment);
            RaiseStateChanged();
        }
    }

    public void AddProcedureStepLocationAssignment(ProcedureStepLocationAssignment assignment)
    {
        lock (_lock)
        {
            _procedureStepLocationAssignments.Add(assignment);
            RaiseStateChanged();
        }
    }

    public void AddProcedureStepAttachmentAssignment(ProcedureStepAttachmentAssignment assignment)
    {
        lock (_lock)
        {
            _procedureStepAttachmentAssignments.Add(assignment);
            RaiseStateChanged();
        }
    }

    public void AddProcedureVersionSnapshot(ProcedureVersionSnapshotRecord snapshot)
    {
        lock (_lock)
        {
            ValidateJson(snapshot.SnapshotJson, "snapshot_json");
            _procedureVersionSnapshots.Add(snapshot);
            RaiseStateChanged();
        }
    }

    public void AddOrUpdateProcedureVersionDiff(ProcedureVersionDiffRecord diff)
    {
        lock (_lock)
        {
            ValidateJson(diff.DiffJson, "diff_json");
            var idx = _procedureVersionDiffRecords.FindIndex(item =>
                item.FromVersionId == diff.FromVersionId &&
                item.ToVersionId == diff.ToVersionId);
            if (idx >= 0)
            {
                _procedureVersionDiffRecords[idx] = diff;
            }
            else
            {
                _procedureVersionDiffRecords.Add(diff);
            }
            RaiseStateChanged();
        }
    }
}
