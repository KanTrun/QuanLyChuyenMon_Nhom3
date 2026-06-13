using Sql = TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Models.Admin;

public sealed class ProcedureSectionDraft
{
    public required int Order { get; init; }
    public required string Number { get; init; }
    public required string Title { get; init; }
    public required string Kind { get; init; }
    public bool IsRequired { get; init; } = true;
    public string Content { get; set; } = string.Empty;
}

public sealed class ProcedureFlowStepDraft
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string RoleId { get; set; } = string.Empty;
    public string Responsibility { get; set; } = string.Empty;
    public string ShapeCode { get; set; } = "process";
    public string FormReference { get; set; } = string.Empty;
    public string DetailSectionNumber { get; set; } = string.Empty;
    public int Minutes { get; set; } = 5;
    public string Description { get; set; } = string.Empty;
}

public sealed class ProcedureRecipientDraft
{
    public string Name { get; set; } = string.Empty;
    public bool IsMarked { get; set; } = true;
}

public sealed class ProcedureRevisionDraft
{
    public DateTime? RevisionDate { get; set; } = DateTime.Today;
    public string PageReference { get; set; } = string.Empty;
    public string SectionReference { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}

public sealed record ProcedureStoredAttachmentDraft(
    string AttachmentType,
    string FileName,
    string FileUri,
    string? MimeType,
    long? FileSizeBytes,
    string? ChecksumSha256);

public sealed record ProcedureAuthoringCommand(
    Guid VersionId,
    Guid? ProcedureId,
    Guid? SourceVersionId,
    string Code,
    string Name,
    string ProcedureType,
    Guid DepartmentId,
    string? Description,
    string? SummaryText,
    DateTime IssueDate,
    int IssueNumber,
    Guid UserId,
    IReadOnlyList<ProcedureSectionDraft> Sections,
    IReadOnlyList<ProcedureRecipientDraft> Recipients,
    IReadOnlyList<ProcedureRevisionDraft> Revisions,
    IReadOnlyList<ProcedureFlowStepDraft> Steps,
    IReadOnlyList<ProcedureStoredAttachmentDraft> Attachments);

public sealed record ProcedureAuthoringResult(
    Sql.ProfessionalProcedure Procedure,
    Sql.ProcedureVersion Version);
