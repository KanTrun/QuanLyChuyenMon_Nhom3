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
                IProcedureAttachmentAccessTokenService accessTokens,
                CancellationToken cancellationToken) =>
            {
                var accessToken = request.Query["accessToken"].FirstOrDefault();
                var sessionToken = request.Headers["X-QLCM-Session"].FirstOrDefault();
                var authorizedByToken = accessTokens.TryValidateToken(attachmentId, accessToken, DateTime.UtcNow);
                var authorizedBySession = false;
                if (sessionTokens.TryValidateToken(sessionToken, out BrowserSessionTokenService.BrowserSessionIdentity sessionIdentity))
                {
                    authorizedBySession = await db.Users.AsNoTracking().AnyAsync(user =>
                        user.UserId == sessionIdentity.UserId &&
                        user.Status == "active" &&
                        user.OnboardingStatus == "active" &&
                        user.DeletedAt == null &&
                        user.ActiveSessionId == sessionIdentity.SessionId,
                        cancellationToken);
                }
                if (!authorizedByToken && !authorizedBySession) return Results.Unauthorized();

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
