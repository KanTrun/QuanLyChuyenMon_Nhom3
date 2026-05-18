using TelemedicineLandingPage.Data;
using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Tests.Admin.Sql;

public sealed class ProcedureLifecycleServiceTests : IDisposable
{
    private readonly MedDbContext _db;
    private readonly ProcedureLifecycleService _svc;
    private readonly Guid _testProcId;

    public ProcedureLifecycleServiceTests()
    {
        _db = TestDbHelper.CreateSeededContext();
        var audit = new AuditTrailService(_db);
        _svc = new ProcedureLifecycleService(_db, audit);

        // Tạo quy trình kiểm thử cục bộ (không phụ thuộc seed)
        _testProcId = Guid.NewGuid();
        _db.Procedures.Add(new ProfessionalProcedure
        {
            ProcedureId = _testProcId,
            ProcedureCode = "QT-TEST-001",
            Name = "Quy trình kiểm thử",
            ProcedureType = "clinical",
            OwnerDepartmentId = MedDataStoreSeed.DeptNoiId,
            Description = "Quy trình dùng cho kiểm thử đơn vị",
            CreatedBy = MedDataStoreSeed.AdminUserId
        });
        _db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    // === 1. CreateDraft: tạo phiên bản mới thành công ===
    [Fact]
    public void CreateDraft_ValidProcedure_ReturnsVersion()
    {
        var ver = _svc.CreateDraft(_testProcId, "Phiên bản kiểm thử", MedDataStoreSeed.AdminUserId);

        Assert.NotNull(ver);
        Assert.Equal("draft", ver.StatusCode);
        Assert.Equal(_testProcId, ver.ProcedureId);
    }

    // === 2. CreateDraft: tự động tăng VersionNo ===
    [Fact]
    public void CreateDraft_IncrementsVersionNo()
    {
        _svc.CreateDraft(_testProcId, "Phiên bản 1", MedDataStoreSeed.AdminUserId);
        var ver = _svc.CreateDraft(_testProcId, "Phiên bản 2", MedDataStoreSeed.AdminUserId);

        Assert.Equal(2, ver.VersionNo);
    }

    // === 3. CreateDraft: quy trình không tồn tại → lỗi ===
    [Fact]
    public void CreateDraft_InvalidProcedure_Throws()
    {
        var ex = Assert.Throws<MedDomainException>(() =>
            _svc.CreateDraft(Guid.NewGuid(), "Không tồn tại", MedDataStoreSeed.AdminUserId));

        Assert.Equal(547, ex.SqlErrorNumber);
    }

    // === 4. Submit: draft → pending_approval ===
    [Fact]
    public void Submit_DraftWithSteps_Succeeds()
    {
        var ver = CreateDraftWithSteps();

        _svc.Submit(ver.ProcedureVersionId, MedDataStoreSeed.AdminUserId);

        var updated = _db.ProcedureVersions
            .First(v => v.ProcedureVersionId == ver.ProcedureVersionId);
        Assert.Equal("pending_approval", updated.StatusCode);
        Assert.NotNull(updated.SubmittedBy);
        Assert.NotNull(updated.SubmittedAt);
    }

    // === 5. Submit: không có bước → lỗi 50021 ===
    [Fact]
    public void Submit_NoSteps_Throws50021()
    {
        var ver = _svc.CreateDraft(_testProcId, "Không có bước", MedDataStoreSeed.AdminUserId);

        var ex = Assert.Throws<MedDomainException>(() =>
            _svc.Submit(ver.ProcedureVersionId, MedDataStoreSeed.AdminUserId));

        Assert.Equal(50021, ex.SqlErrorNumber);
    }

    // === 6. Submit: không phải draft → lỗi 50020 ===
    [Fact]
    public void Submit_NotDraft_Throws50020()
    {
        var ver = CreateDraftWithSteps();
        _svc.Submit(ver.ProcedureVersionId, MedDataStoreSeed.AdminUserId);

        var ex = Assert.Throws<MedDomainException>(() =>
            _svc.Submit(ver.ProcedureVersionId, MedDataStoreSeed.AdminUserId));

        Assert.Equal(50020, ex.SqlErrorNumber);
    }

    // === 7. Publish: pending_approval → published ===
    [Fact]
    public void Publish_PendingVersion_Succeeds()
    {
        var ver = CreateDraftWithSteps();
        _svc.Submit(ver.ProcedureVersionId, MedDataStoreSeed.AdminUserId);

        _svc.Publish(ver.ProcedureVersionId, MedDataStoreSeed.AdminUserId);

        var updated = _db.ProcedureVersions
            .First(v => v.ProcedureVersionId == ver.ProcedureVersionId);
        Assert.Equal("published", updated.StatusCode);
        Assert.NotNull(updated.PublishedAt);
    }

    // === 8. Publish: one-active guard — bản cũ bị superseded ===
    [Fact]
    public void Publish_SupersedesOldPublished()
    {
        var ver1 = CreateDraftWithSteps();
        _svc.Submit(ver1.ProcedureVersionId, MedDataStoreSeed.AdminUserId);
        _svc.Publish(ver1.ProcedureVersionId, MedDataStoreSeed.AdminUserId);

        var ver2 = CreateDraftWithSteps();
        _svc.Submit(ver2.ProcedureVersionId, MedDataStoreSeed.AdminUserId);
        _svc.Publish(ver2.ProcedureVersionId, MedDataStoreSeed.AdminUserId);

        var superseded = _db.ProcedureVersions
            .First(v => v.ProcedureVersionId == ver1.ProcedureVersionId);
        Assert.Equal("superseded", superseded.StatusCode);

        var publishedCount = _db.ProcedureVersions
            .Count(v => v.ProcedureId == _testProcId && v.StatusCode == "published");
        Assert.Equal(1, publishedCount);
    }

    // === 9. Reject: pending_approval → rejected ===
    [Fact]
    public void Reject_PendingVersion_Succeeds()
    {
        var ver = CreateDraftWithSteps();
        _svc.Submit(ver.ProcedureVersionId, MedDataStoreSeed.AdminUserId);

        _svc.Reject(ver.ProcedureVersionId, MedDataStoreSeed.AdminUserId, "Chưa đủ chi tiết");

        var updated = _db.ProcedureVersions
            .First(v => v.ProcedureVersionId == ver.ProcedureVersionId);
        Assert.Equal("rejected", updated.StatusCode);
    }

    // === 10. Withdraw: published → withdrawn ===
    [Fact]
    public void Withdraw_PublishedVersion_Succeeds()
    {
        var ver = CreateDraftWithSteps();
        _svc.Submit(ver.ProcedureVersionId, MedDataStoreSeed.AdminUserId);
        _svc.Publish(ver.ProcedureVersionId, MedDataStoreSeed.AdminUserId);

        _svc.Withdraw(ver.ProcedureVersionId, MedDataStoreSeed.AdminUserId, "Phát hiện lỗi nghiêm trọng");

        var updated = _db.ProcedureVersions
            .First(v => v.ProcedureVersionId == ver.ProcedureVersionId);
        Assert.Equal("withdrawn", updated.StatusCode);
        Assert.NotNull(updated.EffectiveTo);
    }

    // === 11. Withdraw: không phải published → lỗi 50024 ===
    [Fact]
    public void Withdraw_NotPublished_Throws50024()
    {
        var ver = CreateDraftWithSteps();

        var ex = Assert.Throws<MedDomainException>(() =>
            _svc.Withdraw(ver.ProcedureVersionId, MedDataStoreSeed.AdminUserId, "Lý do"));

        Assert.Equal(50024, ex.SqlErrorNumber);
    }

    // === 12. GetActiveVersion: trả về bản published ===
    [Fact]
    public void GetActiveVersion_ReturnsPublished()
    {
        var ver = CreateDraftWithSteps();
        _svc.Submit(ver.ProcedureVersionId, MedDataStoreSeed.AdminUserId);
        _svc.Publish(ver.ProcedureVersionId, MedDataStoreSeed.AdminUserId);

        var active = _svc.GetActiveVersion(_testProcId);

        Assert.NotNull(active);
        Assert.Equal(ver.ProcedureVersionId, active!.ProcedureVersionId);
    }

    // === 13. Audit trail: publish tạo bản ghi kiểm toán ===
    [Fact]
    public void Publish_CreatesAuditLog()
    {
        var ver = CreateDraftWithSteps();
        _svc.Submit(ver.ProcedureVersionId, MedDataStoreSeed.AdminUserId);
        var countBefore = _db.AuditLogs.Count();

        _svc.Publish(ver.ProcedureVersionId, MedDataStoreSeed.AdminUserId);

        Assert.True(_db.AuditLogs.Count() > countBefore);
        var log = _db.AuditLogs.OrderByDescending(a => a.OccurredAt).First();
        Assert.Equal("publish", log.ActionCode);
        Assert.Equal("procedure_version", log.TargetType);
    }

    private ProcedureVersion CreateDraftWithSteps(Guid? procedureId = null)
    {
        var procId = procedureId ?? _testProcId;
        var ver = _svc.CreateDraft(procId, "Phiên bản kiểm thử", MedDataStoreSeed.AdminUserId);

        _db.ProcedureSteps.Add(new ProcedureStep
        {
            ProcedureVersionId = ver.ProcedureVersionId,
            StepNo = 1,
            Name = "Bước kiểm thử 1",
            Description = "Mô tả bước kiểm thử"
        });
        _db.ProcedureSteps.Add(new ProcedureStep
        {
            ProcedureVersionId = ver.ProcedureVersionId,
            StepNo = 2,
            Name = "Bước kiểm thử 2",
            Description = "Mô tả bước kiểm thử thứ hai"
        });
        _db.SaveChanges();

        return ver;
    }
}
