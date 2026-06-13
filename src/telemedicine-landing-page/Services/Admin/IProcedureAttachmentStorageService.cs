using Microsoft.AspNetCore.Components.Forms;

namespace TelemedicineLandingPage.Services.Admin;

public interface IProcedureAttachmentStorageService
{
    Task<StoredProcedureAttachment> SaveAsync(
        Guid versionId,
        IBrowserFile file,
        CancellationToken cancellationToken = default);

    string? ResolveAbsolutePath(string fileUri);
}

public sealed record StoredProcedureAttachment(
    string FileName,
    string FileUri,
    string MimeType,
    long FileSizeBytes,
    string ChecksumSha256);

