using TelemedicineLandingPage.Application.Signature;
using TelemedicineLandingPage.Application.Workflow;
using TelemedicineLandingPage.Data;
using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;
using System.Security.Claims;

namespace TelemedicineLandingPage.Tests.Admin.Sql;

public sealed class SignatureServiceTests
{
    [Fact]
    public async Task CreateDemoSignatureAsync_AppliedApplication_CreatesRecordAndMarksSigned()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var app = AddApplication(db, "applied");
        var service = CreateService(db);

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
        Assert.Equal("signed", db.PatientProtocolApplications.Single(a => a.PatientProtocolApplicationId == app.PatientProtocolApplicationId).ApplicationStatus);
        Assert.Contains(db.AuditLogs, log => log.ActionCode == "sign" && log.TargetId == app.PatientProtocolApplicationId.ToString());
    }

    [Fact]
    public async Task CreateDemoSignatureAsync_AlreadySigned_ReturnsAlreadySignedWithoutSecondRecord()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var app = AddApplication(db, "applied");
        var service = CreateService(db);

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
        Assert.Single(db.SignatureRecords.Where(s => s.TargetId == app.PatientProtocolApplicationId));
    }

    [Fact]
    public async Task CreateDemoSignatureAsync_UserWithoutPermission_ReturnsUnauthorized()
    {
        using var db = TestDbHelper.CreateSeededContext();
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
        var service = CreateService(db);

        var (result, record) = await service.CreateDemoSignatureAsync(
            SignatureService.PatientProtocolApplicationTarget,
            app.PatientProtocolApplicationId,
            user.UserId,
            user.Username);

        Assert.Equal(SignatureResult.Unauthorized, result);
        Assert.Null(record);
        Assert.Empty(db.SignatureRecords);
    }

    [Fact]
    public void VerifyIntegrity_TamperedRecord_ReturnsFalse()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var service = CreateService(db);
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
    public async Task RevokeDemoSignatureAsync_BlankReason_Throws()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var app = AddApplication(db, "signed");
        var service = CreateService(db);

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

    private static SignatureService CreateService(MedDbContext db)
        => new(
            db,
            new EffectivePermissionResolver(db),
            new PatientProtocolApplicationWorkflowGuard(new AuditTrailService(db)));

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
