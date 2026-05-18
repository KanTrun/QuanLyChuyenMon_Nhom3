using TelemedicineLandingPage.Data;
using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Tests.Admin.Sql;

public sealed class PermissionChangeRequestServiceTests : IDisposable
{
    private readonly MedDbContext _db;
    private readonly PermissionChangeRequestService _svc;
    private readonly Guid _testApproverId;

    public PermissionChangeRequestServiceTests()
    {
        _db = TestDbHelper.CreateSeededContext();
        var audit = new AuditTrailService(_db);
        _svc = new PermissionChangeRequestService(_db, audit);

        // Tạo người dùng phê duyệt cục bộ (không phụ thuộc seed)
        _testApproverId = Guid.NewGuid();
        _db.Users.Add(new AppUser
        {
            UserId = _testApproverId,
            Username = "test_approver",
            FullName = "Người phê duyệt kiểm thử",
            PrimaryDepartmentId = MedDataStoreSeed.DeptNoiId
        });
        _db.UserRoles.Add(new UserRole
        {
            UserId = _testApproverId,
            RoleId = MedDataStoreSeed.RoleDeptAdminId,
            DepartmentId = MedDataStoreSeed.DeptNoiId
        });
        _db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    // === 1. CreateDraft: tạo yêu cầu thành công ===
    [Fact]
    public void CreateDraft_ValidInput_ReturnsRequest()
    {
        var req = _svc.CreateDraft(
            MedDataStoreSeed.AdminUserId, "role",
            MedDataStoreSeed.RoleSysAdminId, null, null,
            "Cần thêm quyền quản trị", DateTime.UtcNow.AddDays(1));

        Assert.NotNull(req);
        Assert.Equal("draft", req.ChangeStatus);
        Assert.Equal("role", req.TargetType);
        Assert.Equal(MedDataStoreSeed.RoleSysAdminId, req.TargetRoleId);
    }

    // === 2. CreateDraft: phải chọn đúng 1 đối tượng ===
    [Fact]
    public void CreateDraft_MultipleTargets_Throws50010()
    {
        var ex = Assert.Throws<MedDomainException>(() =>
            _svc.CreateDraft(
                MedDataStoreSeed.AdminUserId, "role",
                MedDataStoreSeed.RoleSysAdminId, Guid.NewGuid(), null,
                "Lý do", DateTime.UtcNow));

        Assert.Equal(50010, ex.SqlErrorNumber);
    }

    // === 3. CreateDraft: lý do không được trống ===
    [Fact]
    public void CreateDraft_EmptyReason_Throws50011()
    {
        var ex = Assert.Throws<MedDomainException>(() =>
            _svc.CreateDraft(
                MedDataStoreSeed.AdminUserId, "role",
                MedDataStoreSeed.RoleSysAdminId, null, null,
                "", DateTime.UtcNow));

        Assert.Equal(50011, ex.SqlErrorNumber);
    }

    // === 4. SubmitForApproval: chuyển draft → pending_approval ===
    [Fact]
    public void SubmitForApproval_WithItems_Succeeds()
    {
        var req = CreateDraftWithItem();

        _svc.SubmitForApproval(req.PermissionChangeRequestId, MedDataStoreSeed.AdminUserId);

        var updated = _db.PermissionChangeRequests
            .First(r => r.PermissionChangeRequestId == req.PermissionChangeRequestId);
        Assert.Equal("pending_approval", updated.ChangeStatus);
    }

    // === 5. SubmitForApproval: không có mục → lỗi 50013 ===
    [Fact]
    public void SubmitForApproval_NoItems_Throws50013()
    {
        var req = _svc.CreateDraft(
            MedDataStoreSeed.AdminUserId, "role",
            MedDataStoreSeed.RoleSysAdminId, null, null,
            "Lý do", DateTime.UtcNow);

        var ex = Assert.Throws<MedDomainException>(() =>
            _svc.SubmitForApproval(req.PermissionChangeRequestId, MedDataStoreSeed.AdminUserId));

        Assert.Equal(50013, ex.SqlErrorNumber);
    }

    // === 6. SubmitForApproval: không phải draft → lỗi 50012 ===
    [Fact]
    public void SubmitForApproval_NotDraft_Throws50012()
    {
        var req = CreateDraftWithItem();
        _svc.SubmitForApproval(req.PermissionChangeRequestId, MedDataStoreSeed.AdminUserId);

        var ex = Assert.Throws<MedDomainException>(() =>
            _svc.SubmitForApproval(req.PermissionChangeRequestId, MedDataStoreSeed.AdminUserId));

        Assert.Equal(50012, ex.SqlErrorNumber);
    }

    // === 7. Approve: chuyển pending_approval → applied ===
    [Fact]
    public void Approve_PendingRequest_Succeeds()
    {
        var req = CreateDraftWithItem();
        _svc.SubmitForApproval(req.PermissionChangeRequestId, MedDataStoreSeed.AdminUserId);

        _svc.Approve(req.PermissionChangeRequestId, _testApproverId);

        var updated = _db.PermissionChangeRequests
            .First(r => r.PermissionChangeRequestId == req.PermissionChangeRequestId);
        Assert.Equal("applied", updated.ChangeStatus);
        Assert.Equal(_testApproverId, updated.ApprovedBy);
    }

    // === 8. Approve: scheduled mode ===
    [Fact]
    public void Approve_Scheduled_SetsScheduledStatus()
    {
        var req = CreateDraftWithItem();
        _svc.SubmitForApproval(req.PermissionChangeRequestId, MedDataStoreSeed.AdminUserId);

        _svc.Approve(req.PermissionChangeRequestId, _testApproverId, schedule: true);

        var updated = _db.PermissionChangeRequests
            .First(r => r.PermissionChangeRequestId == req.PermissionChangeRequestId);
        Assert.Equal("scheduled", updated.ChangeStatus);
    }

    // === 9. Reject: chuyển pending_approval → rejected ===
    [Fact]
    public void Reject_PendingRequest_Succeeds()
    {
        var req = CreateDraftWithItem();
        _svc.SubmitForApproval(req.PermissionChangeRequestId, MedDataStoreSeed.AdminUserId);

        _svc.Reject(req.PermissionChangeRequestId, _testApproverId, "Không phù hợp");

        var updated = _db.PermissionChangeRequests
            .First(r => r.PermissionChangeRequestId == req.PermissionChangeRequestId);
        Assert.Equal("rejected", updated.ChangeStatus);
    }

    // === 10. Cancel: hủy draft thành công ===
    [Fact]
    public void Cancel_DraftRequest_Succeeds()
    {
        var req = _svc.CreateDraft(
            MedDataStoreSeed.AdminUserId, "user",
            null, null, _testApproverId,
            "Thử nghiệm", DateTime.UtcNow);

        _svc.Cancel(req.PermissionChangeRequestId, MedDataStoreSeed.AdminUserId);

        var updated = _db.PermissionChangeRequests
            .First(r => r.PermissionChangeRequestId == req.PermissionChangeRequestId);
        Assert.Equal("cancelled", updated.ChangeStatus);
    }

    // === 11. Cancel: không thể hủy yêu cầu đã applied ===
    [Fact]
    public void Cancel_AppliedRequest_Throws50016()
    {
        var req = CreateDraftWithItem();
        _svc.SubmitForApproval(req.PermissionChangeRequestId, MedDataStoreSeed.AdminUserId);
        _svc.Approve(req.PermissionChangeRequestId, _testApproverId);

        var ex = Assert.Throws<MedDomainException>(() =>
            _svc.Cancel(req.PermissionChangeRequestId, MedDataStoreSeed.AdminUserId));

        Assert.Equal(50016, ex.SqlErrorNumber);
    }

    // === 12. AddItem: chỉ thêm khi draft ===
    [Fact]
    public void AddItem_NotDraft_Throws50017()
    {
        var req = CreateDraftWithItem();
        _svc.SubmitForApproval(req.PermissionChangeRequestId, MedDataStoreSeed.AdminUserId);

        var ex = Assert.Throws<MedDomainException>(() =>
            _svc.AddItem(req.PermissionChangeRequestId, new PermissionChangeItem
            {
                PermissionChangeRequestId = req.PermissionChangeRequestId,
                PermissionId = MedDataStoreSeed.PermViewDashId,
                OperationCode = "grant",
                EffectCode = "allow",
                DepartmentScopeType = "all"
            }));

        Assert.Equal(50017, ex.SqlErrorNumber);
    }

    // === 13. Audit trail: submit tạo bản ghi kiểm toán ===
    [Fact]
    public void SubmitForApproval_CreatesAuditLog()
    {
        var req = CreateDraftWithItem();
        var countBefore = _db.AuditLogs.Count();

        _svc.SubmitForApproval(req.PermissionChangeRequestId, MedDataStoreSeed.AdminUserId);

        Assert.True(_db.AuditLogs.Count() > countBefore);
        var log = _db.AuditLogs.OrderByDescending(a => a.OccurredAt).First();
        Assert.Equal("submit", log.ActionCode);
        Assert.Equal("permission_change_request", log.TargetType);
    }

    private PermissionChangeRequest CreateDraftWithItem()
    {
        var req = _svc.CreateDraft(
            MedDataStoreSeed.AdminUserId, "role",
            MedDataStoreSeed.RoleSysAdminId, null, null,
            "Cần cấp thêm quyền", DateTime.UtcNow.AddDays(1));

        _svc.AddItem(req.PermissionChangeRequestId, new PermissionChangeItem
        {
            PermissionChangeRequestId = req.PermissionChangeRequestId,
            PermissionId = MedDataStoreSeed.PermViewDashId,
            OperationCode = "grant",
            EffectCode = "allow",
            DepartmentScopeType = "all"
        });

        return req;
    }
}
