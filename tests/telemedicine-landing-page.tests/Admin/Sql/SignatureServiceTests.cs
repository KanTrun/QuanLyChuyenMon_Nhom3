using TelemedicineLandingPage.Application.Signature;
using TelemedicineLandingPage.Application.Workflow;
using TelemedicineLandingPage.Data;
using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TelemedicineLandingPage.Tests.Admin.Sql;

public sealed class SignatureServiceTests
{
    private const string ValidPngDataUrl = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";

    [Fact]
    public async Task CreateDemoSignatureAsync_AppliedApplication_CreatesRecordAndMarksSigned()
    {
        var (db, factory) = TestDbHelper.CreateSeededContextWithFactory();
        using var _ = db;
        var app = AddApplication(db, "applied");
        var service = CreateService(factory, db);

        var (result, record) = await service.CreateDemoSignatureAsync(
            SignatureService.PatientProtocolApplicationTarget,
            app.PatientProtocolApplicationId,
            MedDataStoreSeed.AdminUserId,
            "admin",
            "{\"source\":\"test\"}");

        Assert.Equal(SignatureResult.Created, result);
        Assert.NotNull(record);
        Assert.False(record.IsLegallyValid);
        Assert.True(service.VerifyIntegrity(record));
        using var readDb = factory.CreateDbContext();
        Assert.Equal("signed", readDb.PatientProtocolApplications.Single(a => a.PatientProtocolApplicationId == app.PatientProtocolApplicationId).ApplicationStatus);
        Assert.Contains(readDb.AuditLogs, log => log.ActionCode == "sign" && log.TargetId == app.PatientProtocolApplicationId.ToString());
    }

    [Fact]
    public async Task CreateDemoSignatureAsync_AlreadySigned_ReturnsAlreadySignedWithoutSecondRecord()
    {
        var (db, factory) = TestDbHelper.CreateSeededContextWithFactory();
        using var _ = db;
        var app = AddApplication(db, "applied");
        var service = CreateService(factory, db);

        await service.CreateDemoSignatureAsync(
            SignatureService.PatientProtocolApplicationTarget,
            app.PatientProtocolApplicationId,
            MedDataStoreSeed.AdminUserId,
            "admin");
        var (result, record) = await service.CreateDemoSignatureAsync(
            SignatureService.PatientProtocolApplicationTarget,
            app.PatientProtocolApplicationId,
            MedDataStoreSeed.AdminUserId,
            "admin");

        Assert.Equal(SignatureResult.AlreadySigned, result);
        Assert.NotNull(record);
        using var readDb = factory.CreateDbContext();
        Assert.Single(readDb.SignatureRecords.Where(s => s.TargetId == app.PatientProtocolApplicationId));
    }

    [Fact]
    public async Task CreateDemoSignatureAsync_UserWithoutPermission_ReturnsUnauthorized()
    {
        var (db, factory) = TestDbHelper.CreateSeededContextWithFactory();
        using var _ = db;
        var app = AddApplication(db, "applied");
        var user = new AppUser
        {
            Username = "no_sign_permission",
            FullName = "No Sign Permission",
            Status = "active",
            OnboardingStatus = "active"
        };
        db.Users.Add(user);
        db.SaveChanges();
        var service = CreateService(factory, db);

        var (result, record) = await service.CreateDemoSignatureAsync(
            SignatureService.PatientProtocolApplicationTarget,
            app.PatientProtocolApplicationId,
            user.UserId,
            user.Username);

        Assert.Equal(SignatureResult.Unauthorized, result);
        Assert.Null(record);
        using var readDb = factory.CreateDbContext();
        Assert.Empty(readDb.SignatureRecords);
    }

    [Fact]
    public async Task CreateDemoSignatureAsync_UserWithClinicalExecuteAlias_CreatesRecord()
    {
        var (db, factory) = TestDbHelper.CreateSeededContextWithFactory();
        using var _ = db;
        var app = AddApplication(db, "applied");
        var user = new AppUser
        {
            Username = "clinical_execute_only",
            FullName = "Clinical Execute Only",
            Status = "active",
            OnboardingStatus = "active"
        };
        var permission = new MedPermission
        {
            PermissionCode = "SCR_CLINICAL:EXECUTE",
            ScreenId = MedDataStoreSeed.ScreenOrderId,
            ActionCode = "execute"
        };
        db.Users.Add(user);
        db.UserRoles.Add(new UserRole { UserId = user.UserId, RoleId = MedDataStoreSeed.RoleClinicalId });
        db.Permissions.Add(permission);
        db.RolePermissions.Add(new RolePermission { RoleId = MedDataStoreSeed.RoleClinicalId, PermissionId = permission.PermissionId });
        db.SaveChanges();
        var service = CreateService(factory, db);

        var (result, record) = await service.CreateDemoSignatureAsync(
            SignatureService.PatientProtocolApplicationTarget,
            app.PatientProtocolApplicationId,
            user.UserId,
            user.Username);

        Assert.Equal(SignatureResult.Created, result);
        Assert.NotNull(record);
        using var readDb = factory.CreateDbContext();
        Assert.Equal("signed", readDb.PatientProtocolApplications.Single(a => a.PatientProtocolApplicationId == app.PatientProtocolApplicationId).ApplicationStatus);
    }

    [Fact]
    public void VerifyIntegrity_TamperedRecord_ReturnsFalse()
    {
        var (db, factory) = TestDbHelper.CreateSeededContextWithFactory();
        using var _ = db;
        var service = CreateService(factory, db);
        var record = new SignatureRecord
        {
            TargetType = SignatureService.PatientProtocolApplicationTarget,
            TargetId = Guid.NewGuid(),
            SignerUserId = MedDataStoreSeed.AdminUserId,
            SignerUsername = "admin",
            ProviderCode = "demo",
            IsLegallyValid = false,
            SignatureHash = "tampered",
            SignedAt = DateTime.UtcNow
        };

        Assert.False(service.VerifyIntegrity(record));
    }

    [Fact]
    public async Task CreateDemoSignatureAsync_CapturedPngMetadata_BindsMetadataIntoIntegrityHash()
    {
        var (db, factory) = TestDbHelper.CreateSeededContextWithFactory();
        using var _ = db;
        var app = AddApplication(db, "applied");
        var service = CreateService(factory, db);
        var metadata = JsonSerializer.Serialize(new
        {
            SignatureCaptured = true,
            SignatureImageDataUrl = ValidPngDataUrl
        });

        var (result, record) = await service.CreateDemoSignatureAsync(
            SignatureService.PatientProtocolApplicationTarget,
            app.PatientProtocolApplicationId,
            MedDataStoreSeed.AdminUserId,
            "admin",
            metadata);

        Assert.Equal(SignatureResult.Created, result);
        Assert.NotNull(record);
        Assert.Equal(metadata, record.MetadataJson);
        Assert.True(service.VerifyIntegrity(record));
        Assert.False(service.VerifyIntegrity(record with { MetadataJson = "{\"SignatureCaptured\":false}" }));
    }

    [Theory]
    [InlineData("data:image/svg+xml;base64,PHN2Zz48L3N2Zz4=")]
    [InlineData("data:image/png;base64,bm90LWEtcG5n")]
    public async Task CreateDemoSignatureAsync_InvalidSignatureImageMetadata_Throws(string imageDataUrl)
    {
        var (db, factory) = TestDbHelper.CreateSeededContextWithFactory();
        using var _ = db;
        var app = AddApplication(db, "applied");
        var service = CreateService(factory, db);
        var metadata = JsonSerializer.Serialize(new { SignatureImageDataUrl = imageDataUrl });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateDemoSignatureAsync(
                SignatureService.PatientProtocolApplicationTarget,
                app.PatientProtocolApplicationId,
                MedDataStoreSeed.AdminUserId,
                "admin",
                metadata));
    }

    [Fact]
    public async Task CreateDemoSignatureAsync_OversizedSignatureImageMetadata_Throws()
    {
        var (db, factory) = TestDbHelper.CreateSeededContextWithFactory();
        using var _ = db;
        var app = AddApplication(db, "applied");
        var service = CreateService(factory, db);
        var imageDataUrl = "data:image/png;base64," +
            Convert.ToBase64String(new byte[SignatureService.MaxSignatureImageBytes + 1]);
        var metadata = JsonSerializer.Serialize(new { SignatureImageDataUrl = imageDataUrl });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateDemoSignatureAsync(
                SignatureService.PatientProtocolApplicationTarget,
                app.PatientProtocolApplicationId,
                MedDataStoreSeed.AdminUserId,
                "admin",
                metadata));
    }

    [Fact]
    public async Task CreateDemoSignatureAsync_CapturedMarkerWithoutImage_Throws()
    {
        var (db, factory) = TestDbHelper.CreateSeededContextWithFactory();
        using var _ = db;
        var app = AddApplication(db, "applied");
        var service = CreateService(factory, db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateDemoSignatureAsync(
                SignatureService.PatientProtocolApplicationTarget,
                app.PatientProtocolApplicationId,
                MedDataStoreSeed.AdminUserId,
                "admin",
                "{\"SignatureCaptured\":true}"));
    }

    [Fact]
    public void VerifyIntegrity_LegacySignatureHash_ReturnsTrue()
    {
        var (db, factory) = TestDbHelper.CreateSeededContextWithFactory();
        using var _ = db;
        var service = CreateService(factory, db);
        var targetId = Guid.NewGuid();
        var signedAt = new DateTime(2026, 6, 2, 8, 0, 0, DateTimeKind.Utc);
        var payload = $"{SignatureService.PatientProtocolApplicationTarget}:{targetId}:{MedDataStoreSeed.AdminUserId}:{signedAt:O}:demo";
        var legacyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        var record = new SignatureRecord
        {
            TargetType = SignatureService.PatientProtocolApplicationTarget,
            TargetId = targetId,
            SignerUserId = MedDataStoreSeed.AdminUserId,
            SignerUsername = "admin",
            ProviderCode = "demo",
            IsLegallyValid = false,
            SignatureHash = legacyHash,
            SignedAt = signedAt,
            MetadataJson = "{\"source\":\"legacy\"}"
        };

        Assert.True(service.VerifyIntegrity(record));
    }

    [Fact]
    public async Task RevokeDemoSignatureAsync_BlankReason_Throws()
    {
        var (db, factory) = TestDbHelper.CreateSeededContextWithFactory();
        using var _ = db;
        var app = AddApplication(db, "signed");
        var service = CreateService(factory, db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RevokeDemoSignatureAsync(
                SignatureService.PatientProtocolApplicationTarget,
                app.PatientProtocolApplicationId,
                MedDataStoreSeed.AdminUserId,
                "admin",
                ""));
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
