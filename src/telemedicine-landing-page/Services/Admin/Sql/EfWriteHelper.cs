using Microsoft.EntityFrameworkCore;
using TelemedicineLandingPage.Data;
using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin.Sql;

internal static class EfWriteHelper
{
    internal static bool SupportsExecuteUpdate(DbContext db)
        => db.Database.IsRelational()
           && db.Database.ProviderName is { } provider
           && !provider.Contains("InMemory", StringComparison.OrdinalIgnoreCase);

    internal static void UpdateProcedure(MedDbContext db, ProfessionalProcedure proc)
    {
        db.ChangeTracker.Clear();
        if (SupportsExecuteUpdate(db))
        {
            var affected = db.Procedures
                .Where(p => p.ProcedureId == proc.ProcedureId)
                .ExecuteUpdate(setters => setters
                    .SetProperty(p => p.Name, proc.Name)
                    .SetProperty(p => p.ProcedureType, proc.ProcedureType)
                    .SetProperty(p => p.OwnerDepartmentId, proc.OwnerDepartmentId)
                    .SetProperty(p => p.Description, proc.Description)
                    .SetProperty(p => p.UpdatedAt, DateTime.UtcNow));
            if (affected == 0)
                throw new InvalidOperationException("Quy trình không tồn tại.");
            return;
        }

        var existing = db.Procedures.FirstOrDefault(p => p.ProcedureId == proc.ProcedureId)
            ?? throw new InvalidOperationException("Quy trình không tồn tại.");
        db.Procedures.Entry(existing).CurrentValues.SetValues(proc with
        {
            CreatedAt = existing.CreatedAt,
            CreatedBy = existing.CreatedBy,
            UpdatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
        db.ChangeTracker.Clear();
    }

    internal static void UpdateProcedureVersion(MedDbContext db, ProcedureVersion updated)
    {
        db.ChangeTracker.Clear();
        if (SupportsExecuteUpdate(db))
        {
            var affected = db.ProcedureVersions
                .Where(v => v.ProcedureVersionId == updated.ProcedureVersionId)
                .ExecuteUpdate(setters => setters
                    .SetProperty(v => v.DepartmentId, updated.DepartmentId)
                    .SetProperty(v => v.Title, updated.Title)
                    .SetProperty(v => v.Summary, updated.Summary)
                    .SetProperty(v => v.ChangeReason, updated.ChangeReason)
                    .SetProperty(v => v.EffectiveFrom, updated.EffectiveFrom)
                    .SetProperty(v => v.EffectiveTo, updated.EffectiveTo)
                    .SetProperty(v => v.IssueDate, updated.IssueDate)
                    .SetProperty(v => v.IssueNumber, updated.IssueNumber)
                    .SetProperty(v => v.SourcePdfFileName, updated.SourcePdfFileName)
                    .SetProperty(v => v.SourcePdfChecksumSha256, updated.SourcePdfChecksumSha256)
                    .SetProperty(v => v.StatusCode, updated.StatusCode)
                    .SetProperty(v => v.SubmittedBy, updated.SubmittedBy)
                    .SetProperty(v => v.SubmittedAt, updated.SubmittedAt)
                    .SetProperty(v => v.ApprovedBy, updated.ApprovedBy)
                    .SetProperty(v => v.ApprovedAt, updated.ApprovedAt)
                    .SetProperty(v => v.PublishedBy, updated.PublishedBy)
                    .SetProperty(v => v.PublishedAt, updated.PublishedAt)
                    .SetProperty(v => v.RequiredWriterSignatures, updated.RequiredWriterSignatures));
            if (affected == 0)
                throw new InvalidOperationException("Phiên bản quy trình không tồn tại.");
            return;
        }

        var existing = db.ProcedureVersions.FirstOrDefault(v => v.ProcedureVersionId == updated.ProcedureVersionId)
            ?? throw new InvalidOperationException("Phiên bản quy trình không tồn tại.");
        db.ProcedureVersions.Entry(existing).CurrentValues.SetValues(updated);
        db.SaveChanges();
        db.ChangeTracker.Clear();
    }
}
