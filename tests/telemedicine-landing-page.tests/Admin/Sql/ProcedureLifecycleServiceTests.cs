using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Tests.Admin.Sql;

public sealed class ProcedureLifecycleServiceTests
{
    private readonly MedDataStore _store = new();
    private readonly ProcedureLifecycleService _svc;

    public ProcedureLifecycleServiceTests()
    {
        var audit = new AuditTrailService(_store);
        _svc = new ProcedureLifecycleService(_store, audit);
    }

    // === 1. CreateDraft: tạo phiên bản mới thành công ===
    [Fact]
    public void CreateDraft_ValidProcedure_ReturnsVersion()
    {
        var procId = _store.Procedures.First().ProcedureId;

        var ver = _svc.CreateDraft(procId, "Phiên bản kiểm thử", MedDataStoreSeed.UserAnId);

        Assert.NotNull(ver);
        Assert.Equal("draft", ver.StatusCode);
        Assert.Equal(procId, ver.ProcedureId);
    }

    // === 2. CreateDraft: tự động tăng VersionNo ===
    [Fact]
    public void CreateDraft_IncrementsVersionNo()
    {
        var procId = _store.Procedures.First().ProcedureId;
        var existingMax = _store.ProcedureVersions
            .Where(v => v.ProcedureId == procId)
            .Max(v => v.VersionNo);

        var ver = _svc.CreateDraft(procId, "Phiên bản mới", MedDataStoreSeed.UserBinhId);

        Assert.Equal(existingMax + 1, ver.VersionNo);
    }

    // === 3. CreateDraft: quy trình không tồn tại → lỗi ===
    [Fact]
    public void CreateDraft_InvalidProcedure_Throws()
    {
        var ex = Assert.Throws<MedDomainException>(() =>
            _svc.CreateDraft(Guid.NewGuid(), "Không tồn tại", MedDataStoreSeed.UserAnId));

        Assert.Equal(547, ex.SqlErrorNumber);
    }

    // === 4. Submit: draft → pending_approval ===
    [Fact]
    public void Submit_DraftWithSteps_Succeeds()
    {
        var ver = CreateDraftWithSteps();

        _svc.Submit(ver.ProcedureVersionId, MedDataStoreSeed.UserBinhId);

        var updated = _store.ProcedureVersions
            .First(v => v.ProcedureVersionId == ver.ProcedureVersionId);
        Assert.Equal("pending_approval", updated.StatusCode);
        Assert.NotNull(updated.SubmittedBy);
        Assert.NotNull(updated.SubmittedAt);
    }

    // === 5. Submit: không có bước → lỗi 50021 ===
    [Fact]
    public void Submit_NoSteps_Throws50021()
    {
        var procId = _store.Procedures.First().ProcedureId;
        var ver = _svc.CreateDraft(procId, "Không có bước", MedDataStoreSeed.UserAnId);

        var ex = Assert.Throws<MedDomainException>(() =>
            _svc.Submit(ver.ProcedureVersionId, MedDataStoreSeed.UserAnId));

        Assert.Equal(50021, ex.SqlErrorNumber);
    }

    // === 6. Submit: không phải draft → lỗi 50020 ===
    [Fact]
    public void Submit_NotDraft_Throws50020()
    {
        var ver = CreateDraftWithSteps();
        _svc.Submit(ver.ProcedureVersionId, MedDataStoreSeed.UserBinhId);

        var ex = Assert.Throws<MedDomainException>(() =>
            _svc.Submit(ver.ProcedureVersionId, MedDataStoreSeed.UserBinhId));

        Assert.Equal(50020, ex.SqlErrorNumber);
    }

    // === 7. Publish: pending_approval → published ===
    [Fact]
    public void Publish_PendingVersion_Succeeds()
    {
        var ver = CreateDraftWithSteps();
        _svc.Submit(ver.ProcedureVersionId, MedDataStoreSeed.UserBinhId);

        _svc.Publish(ver.ProcedureVersionId, MedDataStoreSeed.UserAnId);

        var updated = _store.ProcedureVersions
            .First(v => v.ProcedureVersionId == ver.ProcedureVersionId);
        Assert.Equal("published", updated.StatusCode);
        Assert.NotNull(updated.PublishedAt);
    }

    // === 8. Publish: one-active guard — bản cũ bị superseded ===
    [Fact]
    public void Publish_SupersedesOldPublished()
    {
        var procId = _store.Procedures.First().ProcedureId;

        // Tìm phiên bản published hiện tại (nếu có)
        var oldPublished = _store.ProcedureVersions
            .FirstOrDefault(v => v.ProcedureId == procId && v.StatusCode == "published");

        var ver = CreateDraftWithSteps(procId);
        _svc.Submit(ver.ProcedureVersionId, MedDataStoreSeed.UserBinhId);
        _svc.Publish(ver.ProcedureVersionId, MedDataStoreSeed.UserAnId);

        if (oldPublished is not null)
        {
            var superseded = _store.ProcedureVersions
                .First(v => v.ProcedureVersionId == oldPublished.ProcedureVersionId);
            Assert.Equal("superseded", superseded.StatusCode);
        }

        // Chỉ có 1 bản published cho quy trình này
        var publishedCount = _store.ProcedureVersions
            .Count(v => v.ProcedureId == procId && v.StatusCode == "published");
        Assert.Equal(1, publishedCount);
    }

    // === 9. Reject: pending_approval → rejected ===
    [Fact]
    public void Reject_PendingVersion_Succeeds()
    {
        var ver = CreateDraftWithSteps();
        _svc.Submit(ver.ProcedureVersionId, MedDataStoreSeed.UserBinhId);

        _svc.Reject(ver.ProcedureVersionId, MedDataStoreSeed.UserAnId, "Chưa đủ chi tiết");

        var updated = _store.ProcedureVersions
            .First(v => v.ProcedureVersionId == ver.ProcedureVersionId);
        Assert.Equal("rejected", updated.StatusCode);
    }

    // === 10. Withdraw: published → withdrawn ===
    [Fact]
    public void Withdraw_PublishedVersion_Succeeds()
    {
        var ver = CreateDraftWithSteps();
        _svc.Submit(ver.ProcedureVersionId, MedDataStoreSeed.UserBinhId);
        _svc.Publish(ver.ProcedureVersionId, MedDataStoreSeed.UserAnId);

        _svc.Withdraw(ver.ProcedureVersionId, MedDataStoreSeed.UserAnId, "Phát hiện lỗi nghiêm trọng");

        var updated = _store.ProcedureVersions
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
            _svc.Withdraw(ver.ProcedureVersionId, MedDataStoreSeed.UserAnId, "Lý do"));

        Assert.Equal(50024, ex.SqlErrorNumber);
    }

    // === 12. GetActiveVersion: trả về bản published ===
    [Fact]
    public void GetActiveVersion_ReturnsPublished()
    {
        var procId = _store.Procedures.First().ProcedureId;
        var ver = CreateDraftWithSteps(procId);
        _svc.Submit(ver.ProcedureVersionId, MedDataStoreSeed.UserBinhId);
        _svc.Publish(ver.ProcedureVersionId, MedDataStoreSeed.UserAnId);

        var active = _svc.GetActiveVersion(procId);

        Assert.NotNull(active);
        Assert.Equal(ver.ProcedureVersionId, active!.ProcedureVersionId);
    }

    // === 13. Audit trail: publish tạo bản ghi kiểm toán ===
    [Fact]
    public void Publish_CreatesAuditLog()
    {
        var ver = CreateDraftWithSteps();
        _svc.Submit(ver.ProcedureVersionId, MedDataStoreSeed.UserBinhId);
        var countBefore = _store.AuditLogs.Count;

        _svc.Publish(ver.ProcedureVersionId, MedDataStoreSeed.UserAnId);

        Assert.True(_store.AuditLogs.Count > countBefore);
        var log = _store.AuditLogs.Last();
        Assert.Equal("publish", log.ActionCode);
        Assert.Equal("procedure_version", log.TargetType);
    }

    private ProcedureVersion CreateDraftWithSteps(Guid? procedureId = null)
    {
        var procId = procedureId ?? _store.Procedures.First().ProcedureId;
        var ver = _svc.CreateDraft(procId, "Phiên bản kiểm thử", MedDataStoreSeed.UserBinhId);

        _store.AddProcedureStep(new ProcedureStep
        {
            ProcedureVersionId = ver.ProcedureVersionId,
            StepNo = 1,
            Name = "Bước kiểm thử 1",
            Description = "Mô tả bước kiểm thử"
        });
        _store.AddProcedureStep(new ProcedureStep
        {
            ProcedureVersionId = ver.ProcedureVersionId,
            StepNo = 2,
            Name = "Bước kiểm thử 2",
            Description = "Mô tả bước kiểm thử thứ hai"
        });

        return ver;
    }
}
