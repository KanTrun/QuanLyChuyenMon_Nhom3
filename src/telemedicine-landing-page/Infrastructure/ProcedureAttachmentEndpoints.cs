using Microsoft.EntityFrameworkCore;
using TelemedicineLandingPage.Data;
using TelemedicineLandingPage.Services.Admin;
using TelemedicineLandingPage.Services.Auth;

namespace TelemedicineLandingPage.Infrastructure;

public static class ProcedureAttachmentEndpoints
{
    public static IEndpointRouteBuilder MapProcedureAttachmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/procedure-attachments/{attachmentId:guid}", async (
                Guid attachmentId,
                HttpRequest request,
                MedDbContext db,
                IProcedureAttachmentStorageService storage,
                BrowserSessionTokenService sessionTokens,
                CancellationToken cancellationToken) =>
            {
                var sessionToken = request.Headers["X-QLCM-Session"].FirstOrDefault();
                if (!sessionTokens.TryValidateToken(sessionToken, out _)) return Results.Unauthorized();

                var attachment = await db.ProcedureAttachments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.ProcedureAttachmentId == attachmentId, cancellationToken);
                if (attachment is null) return Results.NotFound();

                var absolutePath = storage.ResolveAbsolutePath(attachment.FileUri);
                return absolutePath is null
                    ? Results.NotFound()
                    : Results.File(absolutePath, attachment.MimeType ?? "application/octet-stream", attachment.FileName);
            });

        return endpoints;
    }
}
