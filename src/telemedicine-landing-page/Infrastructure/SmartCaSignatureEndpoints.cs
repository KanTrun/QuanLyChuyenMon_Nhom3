using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TelemedicineLandingPage.Application.Signature;
using TelemedicineLandingPage.Services.Auth;

namespace TelemedicineLandingPage.Infrastructure;

public static class SmartCaSignatureEndpoints
{
    private const string CallbackSecretHeader = "X-QLCM-SMARTCA-CALLBACK-SECRET";

    public static IEndpointRouteBuilder MapSmartCaSignatureEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/signatures/smartca")
            .WithTags("SmartCA Signatures")
            .RequireAuthorization();

        group.MapGet("/readiness", (ISignatureService signatures) =>
            Results.Ok(SmartCaReadinessApiResponse.From(signatures.GetSmartCaReadiness())));

        group.MapGet("/transactions/latest", async (
            [FromQuery] string targetType,
            [FromQuery] Guid targetId,
            ClaimsPrincipal user,
            ISignatureService signatures,
            CancellationToken cancellationToken) =>
        {
            if (!TryResolveActor(user, out var actor))
                return Results.Unauthorized();

            var transaction = await signatures.GetLatestSmartCaTransactionAsync(
                targetType,
                targetId,
                actor.UserId,
                cancellationToken);

            return transaction is null
                ? Results.NotFound()
                : Results.Ok(SmartCaSignatureApiResponse.From(SignatureResult.PendingExternalConfirmation, transaction));
        });

        group.MapPost("/start", async (
            SmartCaStartSignatureApiRequest request,
            ClaimsPrincipal user,
            ISignatureService signatures,
            CancellationToken cancellationToken) =>
        {
            if (!TryResolveActor(user, out var actor))
                return Results.Unauthorized();

            var (result, transaction) = await signatures.StartSmartCaSignatureAsync(
                request.TargetType,
                request.TargetId,
                actor.UserId,
                actor.Username,
                request.MetadataJson,
                cancellationToken);

            return ResultFor(result, SmartCaSignatureApiResponse.From(result, transaction));
        });

        group.MapPost("/transactions/{signatureTransactionId:guid}/refresh", async (
            Guid signatureTransactionId,
            ClaimsPrincipal user,
            ISignatureService signatures,
            CancellationToken cancellationToken) =>
        {
            if (!TryResolveActor(user, out var actor))
                return Results.Unauthorized();

            var (result, record, transaction) = await signatures.RefreshSmartCaSignatureAsync(
                signatureTransactionId,
                actor.UserId,
                actor.Username,
                cancellationToken);

            return ResultFor(result, SmartCaSignatureApiResponse.From(result, transaction, record));
        });

        group.MapPost("/callback", async (
            SmartCaCallbackApiRequest request,
            HttpRequest httpRequest,
            IOptions<SmartCaOptions> options,
            ISignatureService signatures,
            CancellationToken cancellationToken) =>
        {
            if (!IsCallbackAuthorized(httpRequest, options.Value))
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            var reference = FirstValue(request.TransactionCode, request.TranCode, request.TransactionId, request.ExternalReference);
            if (string.IsNullOrWhiteSpace(reference))
                return Results.BadRequest(new { message = "Missing SmartCA transaction reference." });

            var (result, record, transaction) = await signatures.RefreshSmartCaSignatureByExternalReferenceAsync(
                reference,
                cancellationToken);

            return ResultFor(result, SmartCaSignatureApiResponse.From(result, transaction, record));
        }).AllowAnonymous();

        return app;
    }

    private static IResult ResultFor(SignatureResult result, SmartCaSignatureApiResponse response)
        => result switch
        {
            SignatureResult.Created => Results.Ok(response),
            SignatureResult.PendingExternalConfirmation => Results.Accepted(value: response),
            SignatureResult.AlreadySigned => Results.Conflict(response),
            SignatureResult.TargetNotFound => Results.NotFound(response),
            SignatureResult.Unauthorized => Results.StatusCode(StatusCodes.Status403Forbidden),
            SignatureResult.InvalidState => Results.Conflict(response),
            SignatureResult.ProviderNotConfigured => Results.Problem(
                title: "SmartCA provider is not configured.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: new Dictionary<string, object?> { ["details"] = response }),
            _ => Results.BadRequest(response)
        };

    private static bool TryResolveActor(ClaimsPrincipal user, out (Guid UserId, string Username) actor)
    {
        var rawUserId = user.FindFirstValue(PermissionClaimTypes.MedUserId)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        var username = user.FindFirstValue(ClaimTypes.Name) ?? user.Identity?.Name;
        if (Guid.TryParse(rawUserId, out var userId) && !string.IsNullOrWhiteSpace(username))
        {
            actor = (userId, username.Trim());
            return true;
        }

        actor = default;
        return false;
    }

    private static bool IsCallbackAuthorized(HttpRequest request, SmartCaOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.CallbackSecret))
            return false;

        var supplied = FirstValue(
            request.Headers[CallbackSecretHeader].FirstOrDefault(),
            request.Headers["X-APP-CB-SECRET"].FirstOrDefault(),
            request.Query["callbackSecret"].FirstOrDefault());
        if (string.IsNullOrWhiteSpace(supplied))
            return false;

        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(options.CallbackSecret.Trim()));
        var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(supplied.Trim()));
        return CryptographicOperations.FixedTimeEquals(expectedHash, suppliedHash);
    }

    private static string? FirstValue(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
}
