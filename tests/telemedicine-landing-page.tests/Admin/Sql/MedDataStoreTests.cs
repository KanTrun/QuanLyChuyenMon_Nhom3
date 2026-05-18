using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Tests.Admin.Sql;

public sealed class MedDataStoreTests
{
    private MedDataStore CreateStore() => new();

    // === 1. Closure table: self-edge tồn tại ===
    [Fact]
    public void AddDepartment_CreatesSelfEdgeInClosure()
    {
        var store = CreateStore();
        var dept = new Department { Code = "TEST-SELF", Name = "Khoa kiểm thử" };
        store.AddDepartment(dept);

        var selfEdge = store.DepartmentClosure
            .FirstOrDefault(e => e.AncestorDepartmentId == dept.DepartmentId
                              && e.DescendantDepartmentId == dept.DepartmentId);
        Assert.NotNull(selfEdge);
        Assert.Equal(0, selfEdge!.Depth);
    }

    // === 2. Closure table: ancestor edges từ cha ===
    [Fact]
    public void AddDepartment_WithParent_CreatesAncestorEdges()
    {
        var store = CreateStore();
        // Sử dụng khoa gốc đã seed
        var rootId = MedDataStoreSeed.RootDeptId;
        var noId = MedDataStoreSeed.DeptNoiId;

        // Thêm khoa con cấp 2 dưới Khoa Nội
        var child = new Department
        {
            Code = "NOI-CON-01",
            Name = "Phân khoa Nội Tim mạch",
            ParentDepartmentId = noId
        };
        store.AddDepartment(child);

        // Phải có edge từ root -> child (depth 2) và noi -> child (depth 1)
        var rootEdge = store.DepartmentClosure
            .FirstOrDefault(e => e.AncestorDepartmentId == rootId
                              && e.DescendantDepartmentId == child.DepartmentId);
        var parentEdge = store.DepartmentClosure
            .FirstOrDefault(e => e.AncestorDepartmentId == noId
                              && e.DescendantDepartmentId == child.DepartmentId);

        Assert.NotNull(rootEdge);
        Assert.Equal(2, rootEdge!.Depth);
        Assert.NotNull(parentEdge);
        Assert.Equal(1, parentEdge!.Depth);
    }

    // === 3. Cycle guard: không thể di chuyển xuống con cháu ===
    [Fact]
    public void UpdateDepartmentParent_CycleGuard_ThrowsError51021()
    {
        var store = CreateStore();
        var rootId = MedDataStoreSeed.RootDeptId;
        var noiId = MedDataStoreSeed.DeptNoiId;

        // Thử di chuyển root xuống dưới Khoa Nội (con của root) → vòng lặp
        var ex = Assert.Throws<MedDomainException>(() =>
        {
            store.UpdateDepartmentParent(rootId, noiId);
        });

        Assert.Equal(51021, ex.SqlErrorNumber);
        Assert.Contains("con cháu", ex.Message);
    }

    // === 4. Archive: không thể lưu trữ khoa có con đang hoạt động ===
    [Fact]
    public void ArchiveDepartment_WithActiveChildren_ThrowsError51024()
    {
        var store = CreateStore();
        var rootId = MedDataStoreSeed.RootDeptId;

        // Root có 7 con đang hoạt động
        var ex = Assert.Throws<MedDomainException>(() =>
        {
            store.ArchiveDepartment(rootId);
        });

        Assert.Equal(51024, ex.SqlErrorNumber);
        Assert.Contains("con đang hoạt động", ex.Message);
    }

    // === 5. Archive: khoa lá có thể lưu trữ thành công ===
    [Fact]
    public void ArchiveDepartment_LeafDepartment_Succeeds()
    {
        var store = CreateStore();
        var leaf = new Department
        {
            Code = "TEST-LEAF",
            Name = "Khoa lá kiểm thử",
            ParentDepartmentId = MedDataStoreSeed.DeptNoiId
        };
        store.AddDepartment(leaf);

        store.ArchiveDepartment(leaf.DepartmentId);

        var archived = store.Departments.First(d => d.DepartmentId == leaf.DepartmentId);
        Assert.Equal("archived", archived.Status);
    }

    // === 6. Audit immutability: AppendAudit chỉ thêm, không sửa ===
    [Fact]
    public void AppendAudit_IsImmutable_CannotModifyExistingLogs()
    {
        var store = CreateStore();
        var log1 = new AuditLog
        {
            CorrelationId = Guid.NewGuid(),
            ActorUserId = MedDataStoreSeed.UserAnId,
            ActionCode = "create",
            TargetType = "department",
            TargetId = "TEST"
        };
        store.AppendAudit(log1);

        var countBefore = store.AuditLogs.Count;
        var log2 = new AuditLog
        {
            CorrelationId = Guid.NewGuid(),
            ActorUserId = MedDataStoreSeed.UserBinhId,
            ActionCode = "update",
            TargetType = "procedure",
            TargetId = "QT-001"
        };
        store.AppendAudit(log2);

        Assert.Equal(countBefore + 1, store.AuditLogs.Count);
        // Bản ghi đầu tiên vẫn nguyên vẹn
        var first = store.AuditLogs.First(a => a.AuditLogId == log1.AuditLogId);
        Assert.Equal("create", first.ActionCode);
        Assert.Equal("department", first.TargetType);
    }

    // === 7. Audit sequence tăng dần ===
    [Fact]
    public void AppendAudit_SequenceIncreases()
    {
        var store = CreateStore();
        store.AppendAudit(new AuditLog { CorrelationId = Guid.NewGuid(), ActionCode = "login", TargetType = "session" });
        store.AppendAudit(new AuditLog { CorrelationId = Guid.NewGuid(), ActionCode = "logout", TargetType = "session" });

        var logs = store.AuditLogs.OrderBy(l => l.AuditLogSeq).ToList();
        for (int i = 1; i < logs.Count; i++)
        {
            Assert.True(logs[i].AuditLogSeq > logs[i - 1].AuditLogSeq);
        }
    }

    // === 8. At-most-one-final: chỉ 1 bản ghi IsFinal cho cùng (order, resource) ===
    [Fact]
    public void AddActualResourceUsage_AtMostOneFinal()
    {
        var store = CreateStore();
        var orderId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var svcId = Guid.NewGuid();

        store.AddTechnicalService(new TechnicalService
        {
            TechnicalServiceId = svcId,
            ServiceCode = "DV-TEST-FINAL",
            Name = "Dịch vụ kiểm thử final",
            ServiceType = "laboratory"
        });
        store.AddTechnicalOrder(new TechnicalOrder
        {
            TechnicalOrderId = orderId,
            TechnicalServiceId = svcId
        });
        store.AddResourceCatalogItem(new ResourceCatalogItem
        {
            ResourceId = resourceId,
            ResourceType = "consumable",
            ResourceCode = "VT-TEST-FINAL",
            Name = "Vật tư kiểm thử"
        });

        // Thêm bản ghi final đầu tiên
        store.AddActualResourceUsage(new ActualResourceUsage
        {
            TechnicalOrderId = orderId,
            ResourceId = resourceId,
            ActualQuantity = 5,
            UnitCode = "cái",
            IsFinal = true
        });

        // Thêm bản ghi final thứ hai → bản ghi đầu bị hạ cấp
        store.AddActualResourceUsage(new ActualResourceUsage
        {
            TechnicalOrderId = orderId,
            ResourceId = resourceId,
            ActualQuantity = 7,
            UnitCode = "cái",
            IsFinal = true
        });

        var finals = store.ActualResourceUsages
            .Where(u => u.TechnicalOrderId == orderId && u.ResourceId == resourceId && u.IsFinal)
            .ToList();
        Assert.Single(finals);
        Assert.Equal(7, finals[0].ActualQuantity);
    }

    // === 9. User deactivation cascade: hết hạn tất cả gán quyền ===
    [Fact]
    public void UpdateUser_Deactivate_ExpiresAllSecurityAssignments()
    {
        var store = CreateStore();
        var userId = Guid.NewGuid();
        var roleId = MedDataStoreSeed.RoleClinicalId;

        store.AddUser(new AppUser
        {
            UserId = userId,
            Username = "test_deactivate",
            FullName = "Người dùng kiểm thử",
            PrimaryDepartmentId = MedDataStoreSeed.DeptNoiId
        });
        store.AddUserRole(new UserRole { UserId = userId, RoleId = roleId });

        // Xác nhận role chưa hết hạn
        var roleBefore = store.UserRoles.First(ur => ur.UserId == userId);
        Assert.Null(roleBefore.EffectiveTo);

        // Vô hiệu hóa người dùng
        var user = store.Users.First(u => u.UserId == userId);
        store.UpdateUser(user with { Status = "inactive" });

        // Role phải bị hết hạn
        var roleAfter = store.UserRoles.First(ur => ur.UserId == userId);
        Assert.NotNull(roleAfter.EffectiveTo);
    }

    // === 10. Duplicate code constraint ===
    [Fact]
    public void AddDepartment_DuplicateCode_Throws()
    {
        var store = CreateStore();
        var ex = Assert.Throws<MedDomainException>(() =>
            store.AddDepartment(new Department { Code = "KHOA-NOI", Name = "Trùng mã" }));
        Assert.Equal(2627, ex.SqlErrorNumber);
    }

    // === 11. RBAC: EffectivePermissionResolver deny-wins-on-tie ===
    [Fact]
    public void PermissionResolver_DenyWinsOnTie()
    {
        var store = CreateStore();
        var resolver = new EffectivePermissionResolver(store);

        // Thêm user override deny cho SYSTEM_ADMIN user trên PERM_CREATE_ORDER
        store.AddUserPermissionOverride(new UserPermissionOverride
        {
            UserId = MedDataStoreSeed.UserAnId,
            PermissionId = MedDataStoreSeed.PermCreateOrderId,
            EffectCode = "deny",
            Priority = 100,
            Reason = "Kiểm thử deny-wins"
        });

        var result = resolver.HasPermission(MedDataStoreSeed.UserAnId, "PERM_CREATE_ORDER");
        // user_override (rank 3) deny phải thắng role (rank 1) allow
        Assert.False(result);
    }

    // === 12. RBAC: source_rank ưu tiên cao hơn ===
    [Fact]
    public void PermissionResolver_HigherSourceRankWins()
    {
        var store = CreateStore();
        var resolver = new EffectivePermissionResolver(store);

        // SYSTEM_ADMIN user (An) có PERM_VIEW_DASHBOARD từ role (allow, rank 1)
        // Thêm user override allow với priority thấp hơn → vẫn thắng vì rank cao hơn
        store.AddUserPermissionOverride(new UserPermissionOverride
        {
            UserId = MedDataStoreSeed.UserAnId,
            PermissionId = MedDataStoreSeed.PermViewDashId,
            EffectCode = "allow",
            Priority = 1, // priority thấp nhưng source_rank = 3
            Reason = "Kiểm thử source_rank"
        });

        var resolved = resolver.Resolve(MedDataStoreSeed.UserAnId);
        var dashPerm = resolved.First(r => r.PermissionCode == "PERM_VIEW_DASHBOARD");
        Assert.Equal(3, dashPerm.SourceRank);
        Assert.Equal("allow", dashPerm.EffectCode);
    }

    // === 13. AuditTrailService: invalid action_code bị từ chối ===
    [Fact]
    public void AuditTrailService_InvalidActionCode_Throws()
    {
        var store = CreateStore();
        var auditService = new AuditTrailService(store);

        var ex = Assert.Throws<ArgumentException>(() =>
            auditService.Append(new AuditLog
            {
                CorrelationId = Guid.NewGuid(),
                ActionCode = "invalid_action_xyz",
                TargetType = "test"
            }));
        Assert.Contains("không hợp lệ", ex.Message);
    }

    // === 14. AuditTrailService: valid action_code ghi thành công ===
    [Fact]
    public void AuditTrailService_ValidActionCode_Succeeds()
    {
        var store = CreateStore();
        var auditService = new AuditTrailService(store);
        var countBefore = store.AuditLogs.Count;

        auditService.Append(new AuditLog
        {
            CorrelationId = Guid.NewGuid(),
            ActorUserId = MedDataStoreSeed.UserAnId,
            ActionCode = "create",
            TargetType = "procedure",
            TargetId = "QT-TEST"
        });

        Assert.Equal(countBefore + 1, store.AuditLogs.Count);
    }

    // === 15. Seed data: đủ 8 departments (1 root + 7 con) ===
    [Fact]
    public void Seed_Creates8Departments()
    {
        var store = CreateStore();
        Assert.Equal(8, store.Departments.Count);
    }

    // === 16. Seed data: đủ 12 users ===
    [Fact]
    public void Seed_Creates12Users()
    {
        var store = CreateStore();
        Assert.True(store.Users.Count >= 12);
    }
}
