using TelemedicineLandingPage.Data;
using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Tests.Admin.Sql;

public sealed class MedDataStoreTests
{
    private MedDataStore CreateStore() => new();

    [Fact]
    public void KsnkSeed_UsesVietnameseAndCanonicalV01Label()
    {
        var store = CreateStore();
        var procedure = store.Procedures.Single(item => item.ProcedureCode == "QT.KSNK.09");
        var version = store.ProcedureVersions.Single(item => item.ProcedureId == procedure.ProcedureId);

        Assert.Equal("Quy trình xử lý dụng cụ phẫu thuật", procedure.Name);
        Assert.Contains("đang chờ OCR đầy đủ", procedure.Description);
        Assert.Equal("v01", version.VersionLabel);
        Assert.Contains(store.ProcedureDocumentSections, item => item.ProcedureVersionId == version.ProcedureVersionId && item.Title == "Mục đích");
        Assert.Contains(store.ProcedureSteps, item => item.ProcedureVersionId == version.ProcedureVersionId && item.Name == "Tiệt khuẩn dụng cụ");
        Assert.Contains(store.ProcedureDistributionRecipients, item => item.ProcedureVersionId == version.ProcedureVersionId && item.RecipientName == "Ban Giám đốc");
    }

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
            ActorUserId = MedDataStoreSeed.AdminUserId,
            ActionCode = "create",
            TargetType = "department",
            TargetId = "TEST"
        };
        store.AppendAudit(log1);

        var countBefore = store.AuditLogs.Count;
        var log2 = new AuditLog
        {
            CorrelationId = Guid.NewGuid(),
            ActorUserId = MedDataStoreSeed.AdminUserId,
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

    [Fact]
    public void ArchiveGroup_ExpiresAssignmentsAndRejectsArchivedMutation()
    {
        var store = CreateStore();
        var groupId = Guid.NewGuid();

        store.AddGroup(new Group
        {
            GroupId = groupId,
            Code = "GROUP-ARCHIVE-GUARD",
            Name = "Nhom kiem thu luu tru",
            DepartmentId = MedDataStoreSeed.DeptNoiId
        });
        store.AddUserGroupMember(new UserGroupMember
        {
            UserId = MedDataStoreSeed.AdminUserId,
            GroupId = groupId
        });
        store.AddGroupPermission(new GroupPermission
        {
            GroupId = groupId,
            PermissionId = MedDataStoreSeed.PermManagePermId
        });

        var membershipId = store.UserGroupMembers.First(m => m.GroupId == groupId).UserGroupMemberId;
        var groupPermissionId = store.GroupPermissions.First(p => p.GroupId == groupId).GroupPermissionId;

        store.ArchiveGroup(groupId);

        Assert.NotNull(store.UserGroupMembers.First(m => m.UserGroupMemberId == membershipId).EffectiveTo);
        Assert.NotNull(store.GroupPermissions.First(p => p.GroupPermissionId == groupPermissionId).EffectiveTo);

        AssertArchivedMutationRejected(() => store.AddUserGroupMember(new UserGroupMember
        {
            UserId = MedDataStoreSeed.AdminUserId,
            GroupId = groupId
        }));
        AssertArchivedMutationRejected(() => store.RemoveUserGroupMember(membershipId));
        AssertArchivedMutationRejected(() => store.AddGroupPermission(new GroupPermission
        {
            GroupId = groupId,
            PermissionId = MedDataStoreSeed.PermViewDashId
        }));
        AssertArchivedMutationRejected(() => store.RemoveGroupPermission(groupPermissionId));
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
        using var db = TestDbHelper.CreateSeededContext();
        var resolver = new EffectivePermissionResolver(db);

        // Thêm user override deny cho admin user trên PERM_CREATE_ORDER
        db.UserPermissionOverrides.Add(new UserPermissionOverride
        {
            UserId = MedDataStoreSeed.AdminUserId,
            PermissionId = MedDataStoreSeed.PermCreateOrderId,
            EffectCode = "deny",
            Priority = 100,
            Reason = "Kiểm thử deny-wins"
        });
        db.SaveChanges();

        var result = resolver.HasPermission(MedDataStoreSeed.AdminUserId, "PERM_CREATE_ORDER");
        // user_override (rank 3) deny phải thắng role (rank 1) allow
        Assert.False(result);
    }

    // === 12. RBAC: priority khớp hàm SQL, thắng source_rank ===
    [Fact]
    public void PermissionResolver_HigherPriorityWinsBeforeSourceRank()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var resolver = new EffectivePermissionResolver(db);

        // Admin user có PERM_VIEW_DASHBOARD từ role (allow, rank 1)
        // Thêm user override deny với priority thấp hơn → role vẫn thắng vì SQL ưu tiên priority trước source_rank
        db.UserPermissionOverrides.Add(new UserPermissionOverride
        {
            UserId = MedDataStoreSeed.AdminUserId,
            PermissionId = MedDataStoreSeed.PermViewDashId,
            EffectCode = "deny",
            Priority = 1,
            Reason = "Kiểm thử priority"
        });
        db.SaveChanges();

        var resolved = resolver.Resolve(MedDataStoreSeed.AdminUserId);
        var dashPerm = resolved.First(r => r.PermissionCode == "PERM_VIEW_DASHBOARD");
        Assert.Equal(1, dashPerm.SourceRank);
        Assert.Equal("allow", dashPerm.EffectCode);
    }

    // === 13. RBAC: department scope phải khớp context khoa/phòng ===
    [Fact]
    public void PermissionResolver_DepartmentScopeRequiresMatchingContext()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var resolver = new EffectivePermissionResolver(db);

        db.RolePermissions.Add(new RolePermission
        {
            RoleId = MedDataStoreSeed.RoleSysAdminId,
            PermissionId = MedDataStoreSeed.PermManagePermId,
            EffectCode = "deny",
            DepartmentScopeType = "department",
            DepartmentId = MedDataStoreSeed.DeptNoiId,
            Priority = 500,
            Reason = "Deny chỉ trong khoa nội"
        });
        db.SaveChanges();

        Assert.False(resolver.HasPermission(MedDataStoreSeed.AdminUserId, "PERM_MANAGE_PERM", MedDataStoreSeed.DeptNoiId));
        Assert.True(resolver.HasPermission(MedDataStoreSeed.AdminUserId, "PERM_MANAGE_PERM", MedDataStoreSeed.DeptNgoaiId));
    }

    // === 14. AuditTrailService: invalid action_code bị từ chối ===
    [Fact]
    public void AuditTrailService_InvalidActionCode_Throws()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var auditService = new AuditTrailService(db);

        var ex = Assert.Throws<ArgumentException>(() =>
            auditService.Append(new AuditLog
            {
                CorrelationId = Guid.NewGuid(),
                ActionCode = "invalid_action_xyz",
                TargetType = "test"
            }));
        Assert.Contains("không hợp lệ", ex.Message);
    }

    // === 15. AuditTrailService: valid action_code ghi thành công ===
    [Fact]
    public void AuditTrailService_ValidActionCode_Succeeds()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var auditService = new AuditTrailService(db);
        var countBefore = db.AuditLogs.Count();

        auditService.Append(new AuditLog
        {
            CorrelationId = Guid.NewGuid(),
            ActorUserId = MedDataStoreSeed.AdminUserId,
            ActionCode = "create",
            TargetType = "procedure",
            TargetId = "QT-TEST"
        });

        Assert.Equal(countBefore + 1, db.AuditLogs.Count());
    }

    // === 16. Seed data: đủ 8 departments (1 root + 7 con) ===
    [Fact]
    public void Seed_Creates8Departments()
    {
        var store = CreateStore();
        Assert.Equal(8, store.Departments.Count);
    }

    // === 17. Seed data: có 10 người dùng ===
    [Fact]
    public void Seed_Creates10Users()
    {
        var store = CreateStore();
        Assert.Equal(10, store.Users.Count);
        var admin = store.Users.First(u => u.UserId == MedDataStoreSeed.AdminUserId);
        Assert.Equal("admin", admin.Username);
        Assert.Equal("Quản trị viên hệ thống", admin.FullName);
    }

    // === 18. Audit tự động: DbContext ghi log khi mutation dữ liệu nghiệp vụ ===
    [Fact]
    public void MedDbContext_SaveChanges_CreatesAutomaticAuditLog()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var roleId = Guid.NewGuid();

        db.Roles.Add(new Role
        {
            RoleId = roleId,
            Code = "AUDIT_TEST_ROLE",
            Name = "Vai trò kiểm thử audit"
        });
        db.SaveChanges();

        var log = db.AuditLogs
            .OrderByDescending(a => a.OccurredAt)
            .FirstOrDefault(a => a.TargetType == "role" && a.TargetId == roleId.ToString());

        Assert.NotNull(log);
        Assert.Equal("create", log!.ActionCode);
        Assert.Contains("AUDIT_TEST_ROLE", log.AfterJson!);
    }

    [Fact]
    public void MedDbContext_SaveChanges_DepartmentAuditDoesNotReferenceNewDepartment()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var departmentId = Guid.NewGuid();

        db.Departments.Add(new Department
        {
            DepartmentId = departmentId,
            Code = "AUDIT-DEPT",
            Name = "Khoa audit",
            ParentDepartmentId = MedDataStoreSeed.RootDeptId
        });
        db.SaveChanges();

        var log = db.AuditLogs.FirstOrDefault(a =>
            a.TargetType == "department" &&
            a.TargetId == departmentId.ToString());

        Assert.NotNull(log);
        Assert.Null(log!.DepartmentId);
    }

    [Fact]
    public void MedDbDataStore_RefreshClearsTrackedRowsAndRaisesStateChanged()
    {
        var (db, factory) = TestDbHelper.CreateSeededContextWithFactory();
        using var _ = db;
        var app = new PatientProtocolApplication
        {
            PatientRefId = Guid.NewGuid(),
            ClinicalProtocolVersionId = Guid.NewGuid(),
            ApplicationStatus = "applied",
            AppliedAt = DateTime.UtcNow
        };
        db.PatientProtocolApplications.Add(app);
        db.SaveChanges();
        var store = new MedDbDataStore(db);
        var raised = false;
        store.StateChanged += () => raised = true;

        Assert.Equal("applied", store.PatientProtocolApplications.Single(a => a.PatientProtocolApplicationId == app.PatientProtocolApplicationId).ApplicationStatus);
        using (var externalDb = factory.CreateDbContext())
        {
            var external = externalDb.PatientProtocolApplications.Single(a => a.PatientProtocolApplicationId == app.PatientProtocolApplicationId);
            externalDb.Entry(external).CurrentValues.SetValues(external with { ApplicationStatus = "signed" });
            externalDb.SaveChanges();
        }

        store.Refresh();

        Assert.True(raised);
        Assert.Equal("signed", store.PatientProtocolApplications.Single(a => a.PatientProtocolApplicationId == app.PatientProtocolApplicationId).ApplicationStatus);
    }

    private static void AssertArchivedMutationRejected(Action action)
    {
        var ex = Assert.Throws<MedDomainException>(action);
        Assert.Equal(50022, ex.SqlErrorNumber);
    }
}
