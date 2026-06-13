using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TelemedicineLandingPage.Application.Signature;
using TelemedicineLandingPage.Application.Workflow;
using TelemedicineLandingPage.Data;
using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Tests.Admin.Sql;

public sealed class SignatureServiceTests
{
    private const string ValidPngDataUrl = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";

    [Fact]
    public async Task CreateInternalSignatureAsync_AppliedApplication_CreatesRecordAndMarksSigned()
    {
        var (db, factory) = TestDbHelper.CreateSeededContextWithFactory();
        using var _ = db;
        var app = AddApplication(db, "applied");
        var service = CreateService(factory, db);

        var (result, record) = await service.CreateInternalSignatureAsync(
            SignatureService.PatientProtocolApplicationTarget,
            app.PatientProtocolApplicationId,
            MedDataStoreSeed.AdminUserId,
            "admin",
            "{\"source\":\"test\"}");

        Assert.Equal(SignatureResult.Created, result);
        Assert.NotNull(record);
        Assert.Equal("internal", record.ProviderCode);
        Assert.False(record.IsLegallyValid);
        Assert.True(service.VerifyIntegrity(record));
        using var readDb = factory.CreateDbContext();
        Assert.Equal("signed", readDb.PatientProtocolApplications.Single(a => a.PatientProtocolApplicationId == app.PatientProtocolApplicationId).ApplicationStatus);
        Assert.Contains(readDb.AuditLogs, log => log.ActionCode == "sign" && log.TargetId == app.PatientProtocolApplicationId.ToString());
    }

    [Fact]
    public async Task CreateInternalSignatureAsync_AlreadySigned_ReturnsAlreadySignedWithoutSecondRecord()
    {
        var (db, factory) = TestDbHelper.CreateSeededContextWithFactory();
        using var _ = db;
        var app = AddApplication(db, "applied");
        var service = CreateService(factory, db);

        await service.CreateInternalSignatureAsync(SignatureService.PatientProtocolApplicationTarget, app.PatientProtocolApplicationId, MedDataStoreSeed.AdminUserId, "admin");
        var (result, record) = await service.CreateInternalSignatureAsync(SignatureService.PatientProtocolApplicationTarget, app.PatientProtocolApplicationId, MedDataStoreSeed.AdminUserId, "admin");

        Assert.Equal(SignatureResult.AlreadySigned, result);
        Assert.NotNull(record);
        using var readDb = factory.CreateDbContext();
        Assert.Single(readDb.SignatureRecords.Where(s => s.TargetId == app.PatientProtocolApplicationId));
    }

    [Fact]
    public async Task CreateInternalSignatureAsync_UserWithoutPermission_ReturnsUnauthorized()
    {
        var (db, factory) = TestDbHelper.CreateSeededContextWithFactory();
        using var _ = db;
        var app = AddApplication(db, "applied");
        var user = new AppUser { Username = "no_sign_permission", FullName = "No Sign Permission", Status = "active", OnboardingStatus = "active" };
        db.Users.Add(user);
        db.SaveChanges();
        var service = CreateService(factory, db);

        var (result, record) = await service.CreateInternalSignatureAsync(
            SignatureService.PatientProtocolApplicationTarget,
            app.PatientProtocolApplicationId,
            user.UserId,
            user.Username);

        Assert.Equal(SignatureResult.Unauthorized, result);
        Assert.Null(record);
    }

    [Fact]
    public async Task CreateInternalSignatureAsync_CapturedPngMetadata_BindsMetadataIntoIntegrityHash()
    {
        var (db, factory) = TestDbHelper.CreateSeededContextWithFactory();
        using var _ = db;
        var app = AddApplication(db, "applied");
        var service = CreateService(factory, db);
        var metadata = JsonSerializer.Serialize(new { SignatureCaptured = true, SignatureImageDataUrl = ValidPngDataUrl });

        var (result, record) = await service.CreateInternalSignatureAsync(
            SignatureService.PatientProtocolApplicationTarget,
            app.PatientProtocolApplicationId,
            MedDataStoreSeed.AdminUserId,
            "admin",
            metadata);

        Assert.Equal(SignatureResult.Created, result);
        Assert.NotNull(record);
        Assert.True(service.VerifyIntegrity(record));
        Assert.False(service.VerifyIntegrity(record with { MetadataJson = "{\"source\":\"changed\"}" }));
    }

    [Theory]
    [InlineData("not-a-data-url")]
    [InlineData("data:image/jpeg;base64,abcd")]
    public async Task CreateInternalSignatureAsync_InvalidSignatureImageMetadata_Throws(string imageDataUrl)
    {
        var (db, factory) = TestDbHelper.CreateSeededContextWithFactory();
        using var _ = db;
        var app = AddApplication(db, "applied");
        var service = CreateService(factory, db);
        var metadata = JsonSerializer.Serialize(new { SignatureCaptured = true, SignatureImageDataUrl = imageDataUrl });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateInternalSignatureAsync(
                SignatureService.PatientProtocolApplicationTarget,
                app.PatientProtocolApplicationId,
                MedDataStoreSeed.AdminUserId,
                "admin",
                metadata));
    }

    [Fact]
    public async Task RevokeInternalSignatureAsync_SignedApplication_Revokes()
    {
        var (db, factory) = TestDbHelper.CreateSeededContextWithFactory();
        using var _ = db;
        var app = AddApplication(db, "applied");
        var service = CreateService(factory, db);
        await service.CreateInternalSignatureAsync(SignatureService.PatientProtocolApplicationTarget, app.PatientProtocolApplicationId, MedDataStoreSeed.AdminUserId, "admin");

        var result = await service.RevokeInternalSignatureAsync(
            SignatureService.PatientProtocolApplicationTarget,
            app.PatientProtocolApplicationId,
            MedDataStoreSeed.AdminUserId,
            "admin",
            "Sai thong tin");

        Assert.Equal(SignatureResult.Revoked, result);
        using var readDb = factory.CreateDbContext();
        Assert.Equal("revoked", readDb.PatientProtocolApplications.Single(a => a.PatientProtocolApplicationId == app.PatientProtocolApplicationId).ApplicationStatus);
    }

    [Fact]
    public void PatientProtocolApplicationWorkflowGuard_NonAdminCannotRevoke()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var guard = new PatientProtocolApplicationWorkflowGuard(new AuditTrailService(db));
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "clinician")
        }, "test"));

        Assert.False(guard.CanTransition("signed", "revoked", user));
        Assert.True(guard.CanTransition("signed", "revoked"));
    }

    private static SignatureService CreateService(IDbContextFactory<MedDbContext> factory, MedDbContext permissionsDb)
        => new(
            factory,
            new EffectivePermissionResolver(permissionsDb),
            new PatientProtocolApplicationWorkflowGuard(new AuditTrailService(permissionsDb)));

    private static PatientProtocolApplication AddApplication(MedDbContext db, string status)
    {
        var app = new PatientProtocolApplication
        {
            PatientRefId = Guid.NewGuid(),
            ClinicalProtocolVersionId = Guid.NewGuid(),
            ApplicationStatus = status,
            AppliedAt = DateTime.UtcNow
        };
        db.PatientProtocolApplications.Add(app);
        db.SaveChanges();
        return app;
    }
}
