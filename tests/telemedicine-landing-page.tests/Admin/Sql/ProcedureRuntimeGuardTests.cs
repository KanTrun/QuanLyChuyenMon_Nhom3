using TelemedicineLandingPage.Data;
using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Tests.Admin.Sql;

public sealed class ProcedureRuntimeGuardTests : IDisposable
{
    private readonly MedDbContext _db;

    public ProcedureRuntimeGuardTests()
    {
        _db = TestDbHelper.CreateSeededContext();
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public void EvaluatePermission_BlockMappingDeniesWhenFirstStepRoleDoesNotMatch()
    {
        var userId = AddUserWithoutRole();
        var versionId = SeedMappedProcedure("block");
        var guard = CreateGuard(userId);

        var decision = guard.EvaluatePermission("SCR_PROCEDURES:UPDATE");

        Assert.False(decision.Allowed);
        Assert.False(decision.WarnOnly);
        Assert.Contains(_db.AuditLogs, log =>
            log.TargetType == "procedure_runtime_guard" &&
            log.TargetId == versionId.ToString());
    }

    [Fact]
    public void EvaluatePermission_WarningMappingAllowsButMarksWarnOnly()
    {
        var userId = AddUserWithoutRole();
        SeedMappedProcedure("warning");
        var guard = CreateGuard(userId);

        var decision = guard.EvaluatePermission("SCR_PROCEDURES:UPDATE");

        Assert.True(decision.Allowed);
        Assert.True(decision.WarnOnly);
    }

    [Fact]
    public void EvaluatePermission_AllowsWhenUserOwnsFirstStepRoleInDepartment()
    {
        var userId = AddUserWithoutRole();
        _db.UserRoles.Add(new UserRole
        {
            UserId = userId,
            RoleId = MedDataStoreSeed.RoleNurseId,
            DepartmentId = MedDataStoreSeed.DeptNoiId
        });
        SeedMappedProcedure("block");
        _db.SaveChanges();
        var guard = CreateGuard(userId);

        var decision = guard.EvaluatePermission("SCR_PROCEDURES:UPDATE");

        Assert.True(decision.Allowed);
        Assert.False(decision.WarnOnly);
    }

    private Guid AddUserWithoutRole()
    {
        var userId = Guid.NewGuid();
        _db.Users.Add(new AppUser
        {
            UserId = userId,
            Username = "runtime_guard_user",
            FullName = "Runtime Guard User",
            PrimaryDepartmentId = MedDataStoreSeed.DeptNoiId
        });
        _db.SaveChanges();
        return userId;
    }

    private Guid SeedMappedProcedure(string enforcementMode)
    {
        var procedureId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        _db.Procedures.Add(new ProfessionalProcedure
        {
            ProcedureId = procedureId,
            ProcedureCode = $"QT-RUNTIME-{enforcementMode.ToUpperInvariant()}",
            Name = "Quy trinh runtime guard",
            ProcedureType = "technical",
            OwnerDepartmentId = MedDataStoreSeed.DeptNoiId
        });
        _db.ProcedureVersions.Add(new ProcedureVersion
        {
            ProcedureVersionId = versionId,
            ProcedureId = procedureId,
            VersionNo = 1,
            StatusCode = "active",
            DepartmentId = MedDataStoreSeed.DeptNoiId,
            Title = "Runtime guard active version",
            EffectiveFrom = DateTime.UtcNow.AddDays(-1)
        });
        _db.ProcedureSteps.Add(new ProcedureStep
        {
            ProcedureVersionId = versionId,
            StepNo = 1,
            Name = "Tiep nhan",
            ActorRoleId = MedDataStoreSeed.RoleNurseId
        });
        _db.ProcedureScreenMappings.Add(new ProcedureScreenMapping
        {
            ProcedureVersionId = versionId,
            ScreenId = MedDataStoreSeed.ScreenProcId,
            ActionCode = "update",
            EnforcementMode = enforcementMode
        });
        _db.SaveChanges();
        return versionId;
    }

    private ProcedureRuntimeGuard CreateGuard(Guid userId)
    {
        var context = new CurrentUserContext(_db, new EffectivePermissionResolver(_db));
        context.SetCurrentUser(userId);
        return new ProcedureRuntimeGuard(new MedDbDataStore(_db), context, new AuditTrailService(_db));
    }
}
