using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>
/// Dữ liệu mẫu toàn diện cho hệ thống Quản Lý Chuyên Môn bệnh viện.
/// Bao gồm: tổ chức, người dùng, quyền hạn, quy trình, dịch vụ kỹ thuật,
/// phác đồ, bệnh nhân, chỉ định và thông báo.
/// </summary>
public static class MedDataStoreSeed
{
    /// <summary>Khởi tạo toàn bộ dữ liệu mẫu vào kho dữ liệu.</summary>
    public static void Apply(MedDataStore store)
    {
        SeedDepartments(store);
        SeedRoles(store);
        SeedUsers(store);
        SeedScreensAndFeatures(store);
        SeedPermissions(store);
        SeedRolePermissions(store);
        SeedProcedures(store);
        SeedTechnicalServices(store);
        SeedClinicalProtocol(store);
        SeedPatientAndEncounter(store);
        SeedTechnicalOrder(store);
        SeedNotifications(store);
    }

    // ═══════════════════════════════════════════════════════════════
    // IDs CỐ ĐỊNH ĐỂ THAM CHIẾU CHÉO
    // ═══════════════════════════════════════════════════════════════

    // --- Khoa/Phòng ---
    public static readonly Guid RootDeptId = Guid.Parse("a0000000-0000-0000-0000-000000000001");
    public static readonly Guid DeptNoiId = Guid.Parse("a0000000-0000-0000-0000-000000000010");
    public static readonly Guid DeptNgoaiId = Guid.Parse("a0000000-0000-0000-0000-000000000020");
    public static readonly Guid DeptSanId = Guid.Parse("a0000000-0000-0000-0000-000000000030");
    public static readonly Guid DeptNhiId = Guid.Parse("a0000000-0000-0000-0000-000000000040");
    public static readonly Guid DeptXetNghiemId = Guid.Parse("a0000000-0000-0000-0000-000000000050");
    public static readonly Guid DeptCdhaId = Guid.Parse("a0000000-0000-0000-0000-000000000060");
    public static readonly Guid DeptHcId = Guid.Parse("a0000000-0000-0000-0000-000000000070");

    // --- Vai trò ---
    public static readonly Guid RoleSysAdminId = Guid.Parse("b0000000-0000-0000-0000-000000000001");
    public static readonly Guid RoleDeptAdminId = Guid.Parse("b0000000-0000-0000-0000-000000000002");
    public static readonly Guid RoleClinicalId = Guid.Parse("b0000000-0000-0000-0000-000000000003");
    public static readonly Guid RoleReportId = Guid.Parse("b0000000-0000-0000-0000-000000000004");
    public static readonly Guid RoleNurseId = Guid.Parse("b0000000-0000-0000-0000-000000000005");

    // --- Người dùng (10 tài khoản) ---
    public static readonly Guid AdminUserId = Guid.Parse("c0000000-0000-0000-0000-000000000001");
    public static readonly Guid TruongKhoaNoiId = Guid.Parse("c0000000-0000-0000-0000-000000000002");
    public static readonly Guid TruongKhoaNgoaiId = Guid.Parse("c0000000-0000-0000-0000-000000000003");
    public static readonly Guid BacSiNoiId = Guid.Parse("c0000000-0000-0000-0000-000000000004");
    public static readonly Guid BacSiXnId = Guid.Parse("c0000000-0000-0000-0000-000000000005");
    public static readonly Guid DieuDuongNoiId = Guid.Parse("c0000000-0000-0000-0000-000000000006");
    public static readonly Guid DieuDuongNgoaiId = Guid.Parse("c0000000-0000-0000-0000-000000000007");
    public static readonly Guid LeTanId = Guid.Parse("c0000000-0000-0000-0000-000000000008");
    public static readonly Guid BaoCaoId = Guid.Parse("c0000000-0000-0000-0000-000000000009");
    public static readonly Guid KyThuatVienId = Guid.Parse("c0000000-0000-0000-0000-00000000000a");

    // --- Màn hình ---
    public static readonly Guid ScreenDashId = Guid.Parse("d0000000-0000-0000-0000-000000000001");
    public static readonly Guid ScreenProcId = Guid.Parse("d0000000-0000-0000-0000-000000000002");
    public static readonly Guid ScreenPermId = Guid.Parse("d0000000-0000-0000-0000-000000000003");
    public static readonly Guid ScreenReportId = Guid.Parse("d0000000-0000-0000-0000-000000000004");
    public static readonly Guid ScreenOrderId = Guid.Parse("d0000000-0000-0000-0000-000000000005");

    // --- Quyền hạn ---
    public static readonly Guid PermViewDashId = Guid.Parse("e0000000-0000-0000-0000-000000000001");
    public static readonly Guid PermManageProcId = Guid.Parse("e0000000-0000-0000-0000-000000000002");
    public static readonly Guid PermApproveProcId = Guid.Parse("e0000000-0000-0000-0000-000000000003");
    public static readonly Guid PermManagePermId = Guid.Parse("e0000000-0000-0000-0000-000000000004");
    public static readonly Guid PermViewReportId = Guid.Parse("e0000000-0000-0000-0000-000000000005");
    public static readonly Guid PermCreateOrderId = Guid.Parse("e0000000-0000-0000-0000-000000000006");

    // --- Quy trình ---
    public static readonly Guid ProcNoiId = Guid.Parse("f0000000-0000-0000-0000-000000000001");
    public static readonly Guid ProcXnId = Guid.Parse("f0000000-0000-0000-0000-000000000002");
    public static readonly Guid ProcNoiVersionId = Guid.Parse("f1000000-0000-0000-0000-000000000001");
    public static readonly Guid ProcXnVersionId = Guid.Parse("f1000000-0000-0000-0000-000000000002");
    public static readonly Guid ProcKsnk09Id = Guid.Parse("f0000000-0000-0000-0000-000000000009");
    public static readonly Guid ProcKsnk12Id = Guid.Parse("f0000000-0000-0000-0000-000000000012");
    public static readonly Guid ProcKsnk16Id = Guid.Parse("f0000000-0000-0000-0000-000000000016");
    public static readonly Guid ProcKsnk17Id = Guid.Parse("f0000000-0000-0000-0000-000000000017");
    public static readonly Guid ProcKsnk09VersionId = Guid.Parse("f1000000-0000-0000-0000-000000000009");
    public static readonly Guid ProcKsnk12VersionId = Guid.Parse("f1000000-0000-0000-0000-000000000012");
    public static readonly Guid ProcKsnk16VersionId = Guid.Parse("f1000000-0000-0000-0000-000000000016");
    public static readonly Guid ProcKsnk17VersionId = Guid.Parse("f1000000-0000-0000-0000-000000000017");

    // --- Dịch vụ kỹ thuật & Nguồn lực ---
    public static readonly Guid SvcXnCtmId = Guid.Parse("f2000000-0000-0000-0000-000000000001");
    public static readonly Guid ResOngEdtaId = Guid.Parse("f3000000-0000-0000-0000-000000000001");
    public static readonly Guid ResKimLayMauId = Guid.Parse("f3000000-0000-0000-0000-000000000002");

    // --- Phác đồ lâm sàng ---
    public static readonly Guid ProtocolThaId = Guid.Parse("f4000000-0000-0000-0000-000000000001");
    public static readonly Guid ProtocolThaVersionId = Guid.Parse("f4100000-0000-0000-0000-000000000001");

    // --- Bệnh nhân & Lượt khám ---
    public static readonly Guid PatientMauId = Guid.Parse("f5000000-0000-0000-0000-000000000001");
    public static readonly Guid EncounterMauId = Guid.Parse("f5100000-0000-0000-0000-000000000001");

    // --- Chỉ định kỹ thuật ---
    public static readonly Guid OrderCtmId = Guid.Parse("f6000000-0000-0000-0000-000000000001");

    // ═══════════════════════════════════════════════════════════════
    // SEED METHODS
    // ═══════════════════════════════════════════════════════════════

    private static void SeedDepartments(MedDataStore store)
    {
        store.AddDepartment(new Department
        {
            DepartmentId = RootDeptId,
            Code = "BV-ROOT",
            Name = "Bệnh viện Đa khoa",
            ParentDepartmentId = null
        });
        store.AddDepartment(new Department
        {
            DepartmentId = DeptNoiId,
            Code = "KHOA-NOI",
            Name = "Khoa Nội",
            ParentDepartmentId = RootDeptId
        });
        store.AddDepartment(new Department
        {
            DepartmentId = DeptNgoaiId,
            Code = "KHOA-NGOAI",
            Name = "Khoa Ngoại",
            ParentDepartmentId = RootDeptId
        });
        store.AddDepartment(new Department
        {
            DepartmentId = DeptSanId,
            Code = "KHOA-SAN",
            Name = "Khoa Sản",
            ParentDepartmentId = RootDeptId
        });
        store.AddDepartment(new Department
        {
            DepartmentId = DeptNhiId,
            Code = "KHOA-NHI",
            Name = "Khoa Nhi",
            ParentDepartmentId = RootDeptId
        });
        store.AddDepartment(new Department
        {
            DepartmentId = DeptXetNghiemId,
            Code = "KHOA-XN",
            Name = "Khoa Xét nghiệm",
            ParentDepartmentId = RootDeptId
        });
        store.AddDepartment(new Department
        {
            DepartmentId = DeptCdhaId,
            Code = "KHOA-CDHA",
            Name = "Khoa Chẩn đoán hình ảnh",
            ParentDepartmentId = RootDeptId
        });
        store.AddDepartment(new Department
        {
            DepartmentId = DeptHcId,
            Code = "PHONG-HC",
            Name = "Phòng Hành chính",
            ParentDepartmentId = RootDeptId
        });
    }

    private static void SeedRoles(MedDataStore store)
    {
        store.AddRole(new Role { RoleId = RoleSysAdminId, Code = "SYSTEM_ADMIN", Name = "Quản trị hệ thống", Description = "Toàn quyền quản trị hệ thống", IsSystem = true });
        store.AddRole(new Role { RoleId = RoleDeptAdminId, Code = "DEPARTMENT_ADMIN", Name = "Quản trị khoa/phòng", Description = "Quản trị trong phạm vi khoa/phòng" });
        store.AddRole(new Role { RoleId = RoleClinicalId, Code = "CLINICAL_USER", Name = "Người dùng lâm sàng", Description = "Bác sĩ, dược sĩ thực hiện quy trình chuyên môn" });
        store.AddRole(new Role { RoleId = RoleReportId, Code = "REPORT_VIEWER", Name = "Xem báo cáo", Description = "Chỉ xem báo cáo thống kê" });
        store.AddRole(new Role { RoleId = RoleNurseId, Code = "NURSE", Name = "Điều dưỡng", Description = "Thực hiện y lệnh và chăm sóc bệnh nhân" });
    }

    private static void SeedUsers(MedDataStore store)
    {
        // 1. Quản trị viên hệ thống
        store.AddUser(new AppUser
        {
            UserId = AdminUserId,
            Username = "admin",
            FullName = "Quản trị viên hệ thống",
            Email = "admin@bv.vn",
            PrimaryDepartmentId = RootDeptId,
            PasswordHash = BootstrapAdminDefaults.PasswordHash
        });
        store.AddUserRole(new UserRole { UserId = AdminUserId, RoleId = RoleSysAdminId });

        // 2. Trưởng khoa Nội
        store.AddUser(new AppUser
        {
            UserId = TruongKhoaNoiId,
            Username = "truongkhoa.noi",
            FullName = "Trưởng khoa Nội",
            Email = "truongkhoa.noi@bv.vn",
            PrimaryDepartmentId = DeptNoiId
        });
        store.AddUserRole(new UserRole { UserId = TruongKhoaNoiId, RoleId = RoleDeptAdminId, DepartmentId = DeptNoiId });

        // 3. Trưởng khoa Ngoại
        store.AddUser(new AppUser
        {
            UserId = TruongKhoaNgoaiId,
            Username = "truongkhoa.ngoai",
            FullName = "Trưởng khoa Ngoại",
            Email = "truongkhoa.ngoai@bv.vn",
            PrimaryDepartmentId = DeptNgoaiId
        });
        store.AddUserRole(new UserRole { UserId = TruongKhoaNgoaiId, RoleId = RoleDeptAdminId, DepartmentId = DeptNgoaiId });

        // 4. Bác sĩ Nội khoa
        store.AddUser(new AppUser
        {
            UserId = BacSiNoiId,
            Username = "bacsi.noi",
            FullName = "Bác sĩ Nội khoa",
            Email = "bacsi.noi@bv.vn",
            PrimaryDepartmentId = DeptNoiId
        });
        store.AddUserRole(new UserRole { UserId = BacSiNoiId, RoleId = RoleClinicalId, DepartmentId = DeptNoiId });

        // 5. Bác sĩ Xét nghiệm
        store.AddUser(new AppUser
        {
            UserId = BacSiXnId,
            Username = "bacsi.xn",
            FullName = "Bác sĩ Xét nghiệm",
            Email = "bacsi.xn@bv.vn",
            PrimaryDepartmentId = DeptXetNghiemId
        });
        store.AddUserRole(new UserRole { UserId = BacSiXnId, RoleId = RoleClinicalId, DepartmentId = DeptXetNghiemId });

        // 6. Điều dưỡng Nội
        store.AddUser(new AppUser
        {
            UserId = DieuDuongNoiId,
            Username = "dieuduong.noi",
            FullName = "Điều dưỡng Nội",
            Email = "dieuduong.noi@bv.vn",
            PrimaryDepartmentId = DeptNoiId
        });
        store.AddUserRole(new UserRole { UserId = DieuDuongNoiId, RoleId = RoleNurseId, DepartmentId = DeptNoiId });

        // 7. Điều dưỡng Ngoại
        store.AddUser(new AppUser
        {
            UserId = DieuDuongNgoaiId,
            Username = "dieuduong.ngoai",
            FullName = "Điều dưỡng Ngoại",
            Email = "dieuduong.ngoai@bv.vn",
            PrimaryDepartmentId = DeptNgoaiId
        });
        store.AddUserRole(new UserRole { UserId = DieuDuongNgoaiId, RoleId = RoleNurseId, DepartmentId = DeptNgoaiId });

        // 8. Lễ tân
        store.AddUser(new AppUser
        {
            UserId = LeTanId,
            Username = "letan",
            FullName = "Lễ tân",
            Email = "letan@bv.vn",
            PrimaryDepartmentId = DeptHcId
        });
        store.AddUserRole(new UserRole { UserId = LeTanId, RoleId = RoleNurseId, DepartmentId = DeptHcId });

        // 9. Nhân viên báo cáo
        store.AddUser(new AppUser
        {
            UserId = BaoCaoId,
            Username = "baocao",
            FullName = "Nhân viên báo cáo",
            Email = "baocao@bv.vn",
            PrimaryDepartmentId = RootDeptId
        });
        store.AddUserRole(new UserRole { UserId = BaoCaoId, RoleId = RoleReportId });

        // 10. Kỹ thuật viên CĐHA
        store.AddUser(new AppUser
        {
            UserId = KyThuatVienId,
            Username = "kythuatvien",
            FullName = "Kỹ thuật viên CĐHA",
            Email = "kythuatvien@bv.vn",
            PrimaryDepartmentId = DeptCdhaId
        });
        store.AddUserRole(new UserRole { UserId = KyThuatVienId, RoleId = RoleClinicalId, DepartmentId = DeptCdhaId });
    }

    private static void SeedScreensAndFeatures(MedDataStore store)
    {
        store.AddScreen(new ScreenCatalog { ScreenId = ScreenDashId, ScreenCode = "SCR_DASHBOARD", Name = "Bảng điều khiển", Route = "/admin", ModuleCode = "CORE" });
        store.AddScreen(new ScreenCatalog { ScreenId = ScreenProcId, ScreenCode = "SCR_PROCEDURES", Name = "Quản lý quy trình", Route = "/admin/procedures", ModuleCode = "PROC" });
        store.AddScreen(new ScreenCatalog { ScreenId = ScreenPermId, ScreenCode = "SCR_PERMISSIONS", Name = "Quản lý phân quyền", Route = "/admin/permissions", ModuleCode = "PERM" });
        store.AddScreen(new ScreenCatalog { ScreenId = ScreenReportId, ScreenCode = "SCR_REPORTS", Name = "Báo cáo thống kê", Route = "/admin/reports", ModuleCode = "RPT" });
        store.AddScreen(new ScreenCatalog { ScreenId = ScreenOrderId, ScreenCode = "SCR_ORDERS", Name = "Chỉ định kỹ thuật", Route = "/admin/orders", ModuleCode = "TECH" });

        // Tính năng cho màn hình quy trình
        store.AddFeature(new FeatureCatalog { ScreenId = ScreenProcId, FeatureCode = "FEAT_PROC_CREATE", Name = "Tạo quy trình mới" });
        store.AddFeature(new FeatureCatalog { ScreenId = ScreenProcId, FeatureCode = "FEAT_PROC_EDIT", Name = "Chỉnh sửa quy trình" });
        store.AddFeature(new FeatureCatalog { ScreenId = ScreenProcId, FeatureCode = "FEAT_PROC_APPROVE", Name = "Phê duyệt quy trình" });
        store.AddFeature(new FeatureCatalog { ScreenId = ScreenPermId, FeatureCode = "FEAT_PERM_ASSIGN", Name = "Gán quyền cho vai trò" });
        store.AddFeature(new FeatureCatalog { ScreenId = ScreenReportId, FeatureCode = "FEAT_RPT_EXPORT", Name = "Xuất báo cáo" });
    }

    private static void SeedPermissions(MedDataStore store)
    {
        store.AddPermission(new MedPermission { PermissionId = PermViewDashId, PermissionCode = "PERM_VIEW_DASHBOARD", ScreenId = ScreenDashId, ActionCode = "view", Description = "Xem bảng điều khiển" });
        store.AddPermission(new MedPermission { PermissionId = PermManageProcId, PermissionCode = "PERM_MANAGE_PROC", ScreenId = ScreenProcId, ActionCode = "manage", Description = "Quản lý quy trình chuyên môn" });
        store.AddPermission(new MedPermission { PermissionId = PermApproveProcId, PermissionCode = "PERM_APPROVE_PROC", ScreenId = ScreenProcId, ActionCode = "approve", Description = "Phê duyệt quy trình" });
        store.AddPermission(new MedPermission { PermissionId = PermManagePermId, PermissionCode = "PERM_MANAGE_PERM", ScreenId = ScreenPermId, ActionCode = "manage", Description = "Quản lý phân quyền" });
        store.AddPermission(new MedPermission { PermissionId = PermViewReportId, PermissionCode = "PERM_VIEW_REPORT", ScreenId = ScreenReportId, ActionCode = "view", Description = "Xem báo cáo" });
        store.AddPermission(new MedPermission { PermissionId = PermCreateOrderId, PermissionCode = "PERM_CREATE_ORDER", ScreenId = ScreenOrderId, ActionCode = "create", Description = "Tạo chỉ định kỹ thuật" });
    }

    private static void SeedRolePermissions(MedDataStore store)
    {
        // SYSTEM_ADMIN: toàn quyền
        store.AddRolePermission(new RolePermission { RoleId = RoleSysAdminId, PermissionId = PermViewDashId, Priority = 100 });
        store.AddRolePermission(new RolePermission { RoleId = RoleSysAdminId, PermissionId = PermManageProcId, Priority = 100 });
        store.AddRolePermission(new RolePermission { RoleId = RoleSysAdminId, PermissionId = PermApproveProcId, Priority = 100 });
        store.AddRolePermission(new RolePermission { RoleId = RoleSysAdminId, PermissionId = PermManagePermId, Priority = 100 });
        store.AddRolePermission(new RolePermission { RoleId = RoleSysAdminId, PermissionId = PermViewReportId, Priority = 100 });
        store.AddRolePermission(new RolePermission { RoleId = RoleSysAdminId, PermissionId = PermCreateOrderId, Priority = 100 });

        // DEPARTMENT_ADMIN: quản lý quy trình + xem báo cáo
        store.AddRolePermission(new RolePermission { RoleId = RoleDeptAdminId, PermissionId = PermViewDashId, Priority = 100 });
        store.AddRolePermission(new RolePermission { RoleId = RoleDeptAdminId, PermissionId = PermManageProcId, Priority = 100 });
        store.AddRolePermission(new RolePermission { RoleId = RoleDeptAdminId, PermissionId = PermViewReportId, Priority = 100 });

        // CLINICAL_USER: xem dashboard + tạo chỉ định
        store.AddRolePermission(new RolePermission { RoleId = RoleClinicalId, PermissionId = PermViewDashId, Priority = 100 });
        store.AddRolePermission(new RolePermission { RoleId = RoleClinicalId, PermissionId = PermCreateOrderId, Priority = 100 });

        // REPORT_VIEWER: chỉ xem báo cáo
        store.AddRolePermission(new RolePermission { RoleId = RoleReportId, PermissionId = PermViewDashId, Priority = 100 });
        store.AddRolePermission(new RolePermission { RoleId = RoleReportId, PermissionId = PermViewReportId, Priority = 100 });

        // NURSE: xem dashboard + tạo chỉ định
        store.AddRolePermission(new RolePermission { RoleId = RoleNurseId, PermissionId = PermViewDashId, Priority = 100 });
        store.AddRolePermission(new RolePermission { RoleId = RoleNurseId, PermissionId = PermCreateOrderId, Priority = 50 });
    }

    private static void SeedProcedures(MedDataStore store)
    {
        // === QT-NOI-001: Quy trình khám bệnh nội khoa (published) ===
        store.AddProcedure(new ProfessionalProcedure
        {
            ProcedureId = ProcNoiId,
            ProcedureCode = "QT-NOI-001",
            Name = "Quy trình khám bệnh nội khoa",
            ProcedureType = "technical",
            OwnerDepartmentId = DeptNoiId,
            Description = "Quy trình chuẩn khám bệnh tại Khoa Nội",
            CreatedBy = TruongKhoaNoiId
        });

        store.AddProcedureVersion(new ProcedureVersion
        {
            ProcedureVersionId = ProcNoiVersionId,
            ProcedureId = ProcNoiId,
            VersionNo = 1,
            VersionLabel = "v1.0",
            StatusCode = "active",
            DepartmentId = DeptNoiId,
            Title = "Quy trình khám bệnh nội khoa - Phiên bản 1",
            Summary = "{\"note\":\"Phiên bản đầu tiên được phê duyệt\"}",
            CreatedBy = TruongKhoaNoiId,
            SubmittedBy = TruongKhoaNoiId,
            ApprovedBy = AdminUserId,
            PublishedBy = AdminUserId,
            PublishedAt = DateTime.UtcNow.AddDays(-7),
            EffectiveFrom = DateTime.UtcNow.AddDays(-7)
        });

        // 4 bước cho quy trình nội khoa
        store.AddProcedureStep(new ProcedureStep
        {
            ProcedureVersionId = ProcNoiVersionId,
            StepNo = 1,
            StepCode = "TIEP-NHAN",
            Name = "Tiếp nhận bệnh nhân",
            Description = "Đăng ký, đo sinh hiệu, phân loại ưu tiên",
            ActorRoleId = RoleNurseId,
            StandardDurationMinutes = 10
        });
        store.AddProcedureStep(new ProcedureStep
        {
            ProcedureVersionId = ProcNoiVersionId,
            StepNo = 2,
            StepCode = "KHAM-BENH",
            Name = "Khám lâm sàng",
            Description = "Hỏi bệnh sử, khám thực thể, đánh giá triệu chứng",
            ActorRoleId = RoleClinicalId,
            StandardDurationMinutes = 20
        });
        store.AddProcedureStep(new ProcedureStep
        {
            ProcedureVersionId = ProcNoiVersionId,
            StepNo = 3,
            StepCode = "CHI-DINH",
            Name = "Chỉ định cận lâm sàng",
            Description = "Yêu cầu xét nghiệm, chẩn đoán hình ảnh nếu cần",
            ActorRoleId = RoleClinicalId,
            StandardDurationMinutes = 5
        });
        store.AddProcedureStep(new ProcedureStep
        {
            ProcedureVersionId = ProcNoiVersionId,
            StepNo = 4,
            StepCode = "KET-LUAN",
            Name = "Kết luận và kê đơn",
            Description = "Chẩn đoán, kê đơn thuốc, hẹn tái khám",
            ActorRoleId = RoleClinicalId,
            StandardDurationMinutes = 10
        });

        // === QT-XN-001: Quy trình xét nghiệm công thức máu (draft) ===
        store.AddProcedure(new ProfessionalProcedure
        {
            ProcedureId = ProcXnId,
            ProcedureCode = "QT-XN-001",
            Name = "Quy trình xét nghiệm công thức máu",
            ProcedureType = "technical",
            OwnerDepartmentId = DeptXetNghiemId,
            Description = "Quy trình chuẩn xét nghiệm công thức máu toàn phần",
            CreatedBy = BacSiXnId
        });

        store.AddProcedureVersion(new ProcedureVersion
        {
            ProcedureVersionId = ProcXnVersionId,
            ProcedureId = ProcXnId,
            VersionNo = 1,
            VersionLabel = "v1.0-draft",
            StatusCode = "draft",
            DepartmentId = DeptXetNghiemId,
            Title = "Quy trình xét nghiệm công thức máu - Bản nháp",
            Summary = "{\"note\":\"Đang soạn thảo, chưa gửi phê duyệt\"}",
            CreatedBy = BacSiXnId
        });

        // 4 bước cho quy trình xét nghiệm
        store.AddProcedureStep(new ProcedureStep
        {
            ProcedureVersionId = ProcXnVersionId,
            StepNo = 1,
            StepCode = "NHAN-MAU",
            Name = "Nhận mẫu bệnh phẩm",
            Description = "Kiểm tra thông tin bệnh nhân, nhận và đánh mã mẫu",
            ActorRoleId = RoleNurseId,
            StandardDurationMinutes = 5
        });
        store.AddProcedureStep(new ProcedureStep
        {
            ProcedureVersionId = ProcXnVersionId,
            StepNo = 2,
            StepCode = "CHAY-MAY",
            Name = "Chạy máy phân tích",
            Description = "Đưa mẫu vào máy phân tích huyết học tự động",
            ActorRoleId = RoleClinicalId,
            StandardDurationMinutes = 15
        });
        store.AddProcedureStep(new ProcedureStep
        {
            ProcedureVersionId = ProcXnVersionId,
            StepNo = 3,
            StepCode = "KIEM-TRA",
            Name = "Kiểm tra kết quả",
            Description = "Đối chiếu kết quả với giá trị tham chiếu, kiểm tra bất thường",
            ActorRoleId = RoleClinicalId,
            StandardDurationMinutes = 10
        });
        store.AddProcedureStep(new ProcedureStep
        {
            ProcedureVersionId = ProcXnVersionId,
            StepNo = 4,
            StepCode = "TRA-KET-QUA",
            Name = "Trả kết quả",
            Description = "Ký duyệt và trả kết quả cho khoa lâm sàng",
            ActorRoleId = RoleClinicalId,
            StandardDurationMinutes = 5
        });

        SeedKsnkProcedures(store);
    }

    private static void SeedKsnkProcedures(MedDataStore store)
    {
        SeedKsnkProcedure(
            store,
            ProcKsnk09Id,
            ProcKsnk09VersionId,
            "QT.KSNK.09",
            "Quy trình xử lý dụng cụ phẫu thuật",
            "2145_QUY TRÌNH XỬ LÝ DỤNG CỤ PHẪU THUẬT.pdf",
            "C77CA23EA777CFFE94D28F110AB6A58BB8C630248FF04D1CD22A8A0C718C5C8A",
            30688980,
            ["Làm sạch dụng cụ", "Giao nhận dụng cụ sau khi làm sạch", "Làm sạch, khử khuẩn dụng cụ", "Bảo dưỡng - kiểm tra dụng cụ", "Đóng gói dụng cụ", "Tiệt khuẩn dụng cụ", "Giám sát chất lượng tiệt khuẩn dụng cụ", "Lưu trữ dụng cụ", "Giao nhận dụng cụ sau khi tiệt khuẩn"]);
        SeedKsnkProcedure(
            store,
            ProcKsnk12Id,
            ProcKsnk12VersionId,
            "QT.KSNK.12",
            "Quy trình xử lý dụng cụ y tế",
            "2145_QUY TRÌNH XỬ LÝ DỤNG CỤ Y TẾ.pdf",
            "A81D27EF2338C86280A6F9A8300D5537A6A68BA4A3B74771BB987B9419166F44",
            26591000,
            ["Làm sạch, khử khuẩn dụng cụ", "Giao nhận dụng cụ sau khi làm sạch", "Làm sạch, khử khuẩn dụng cụ", "Bảo dưỡng - kiểm tra dụng cụ", "Đóng gói dụng cụ", "Tiệt khuẩn dụng cụ", "Giám sát chất lượng tiệt khuẩn dụng cụ", "Lưu trữ dụng cụ", "Giao nhận dụng cụ sau khi tiệt khuẩn"]);
        SeedKsnkProcedure(
            store,
            ProcKsnk16Id,
            ProcKsnk16VersionId,
            "QT.KSNK.16",
            "Quy trình khử khuẩn mức độ cao dụng cụ y tế",
            "2145_QUY TRÌNH KHỬ KHUẨN MỨC ĐỘ CAO DỤNG CỤ Y TẾ.pdf",
            "F0E0EE39369E3815FF6634A217555A15F68DD878D040CCBC3A23B23C8631892A",
            11543000,
            ["Làm sạch dụng cụ", "Giao nhận dụng cụ sau khi làm sạch", "Khử khuẩn mức độ cao dụng cụ", "Đóng gói dụng cụ", "Lưu trữ dụng cụ tại khoa KSNK", "Giao nhận dụng cụ vô khuẩn"]);
        SeedKsnkProcedure(
            store,
            ProcKsnk17Id,
            ProcKsnk17VersionId,
            "QT.KSNK.17",
            "Quy trình xử lý tay khoan nha khoa",
            "2145_QUY TRÌNH XỬ LÝ TAY KHOAN NHA KHOA.pdf",
            "40A3241A42BB0B803A75A599B55EEA95D6EC55917CEBF91FCF26F26D717CC4A5",
            6530255,
            ["Chuẩn bị", "Làm sạch", "Khử khuẩn", "Tra dầu bôi trơn", "Giao nhận dụng cụ sau khi làm sạch, khử khuẩn", "Đóng gói", "Tiệt khuẩn", "Lưu trữ tại khoa KSNK", "Giao nhận dụng cụ sau khi tiệt khuẩn"]);
    }

    private static void SeedKsnkProcedure(
        MedDataStore store,
        Guid procedureId,
        Guid versionId,
        string code,
        string title,
        string pdfFileName,
        string checksum,
        long fileSize,
        IReadOnlyList<string> steps)
    {
        store.AddProcedure(new ProfessionalProcedure
        {
            ProcedureId = procedureId,
            ProcedureCode = code,
            Name = title,
            ProcedureType = "technical",
            OwnerDepartmentId = DeptHcId,
            Description = "Trích xuất từ PDF scan 2145; PDF nguồn được gắn kèm làm căn cứ kiểm soát nội dung.",
            CreatedBy = AdminUserId
        });

        store.AddProcedureVersion(new ProcedureVersion
        {
            ProcedureVersionId = versionId,
            ProcedureId = procedureId,
            VersionNo = 1,
            VersionLabel = "v01",
            StatusCode = "draft",
            DepartmentId = DeptHcId,
            Title = title,
            Summary = "{\"ocrStatus\":\"OCR_EXTRACTED\",\"note\":\"Nội dung chính và lưu đồ đã được nhập từ OCR; PDF scan nguồn được giữ kèm để kiểm soát.\"}",
            ChangeReason = "Nhập quy trình KSNK từ PDF scan",
            IssueDate = new DateTime(2026, 3, 19),
            IssueNumber = 2,
            SourcePdfFileName = pdfFileName,
            SourcePdfChecksumSha256 = checksum,
            CreatedBy = AdminUserId
        });

        AddDefaultProcedureSections(store, versionId, code);
        store.AddProcedureDistributionRecipient(new ProcedureDistributionRecipient { ProcedureVersionId = versionId, DisplayOrder = 1, RecipientName = "Ban Giám đốc" });
        store.AddProcedureDistributionRecipient(new ProcedureDistributionRecipient { ProcedureVersionId = versionId, DisplayOrder = 2, RecipientName = "Khoa Kiểm soát nhiễm khuẩn" });
        store.AddProcedureDistributionRecipient(new ProcedureDistributionRecipient { ProcedureVersionId = versionId, DisplayOrder = 3, RecipientName = "Các khoa/phòng sử dụng dụng cụ" });
        store.AddProcedureRevisionEntry(new ProcedureRevisionEntry { ProcedureVersionId = versionId, DisplayOrder = 1, RevisionDate = new DateTime(2026, 3, 19), PageRef = "Toàn văn", SectionRef = "Lần 02", Summary = "Ban hành theo PDF scan số 2145; nội dung chính và lưu đồ đã nhập từ OCR, PDF nguồn được giữ kèm để kiểm soát." });

        var flowDetails = KsnkFlowDetails(code, steps);
        for (var i = 0; i < steps.Count; i++)
        {
            var detail = flowDetails[i];
            store.AddProcedureStep(new ProcedureStep
            {
                ProcedureVersionId = versionId,
                StepNo = i + 1,
                StepCode = $"B{i + 1:00}",
                Name = detail.Name,
                Description = detail.Description,
                ResponsibilityText = detail.Responsibility,
                FlowShapeCode = detail.ShapeCode,
                FormReferenceText = detail.FormReference,
                DetailSectionNumber = detail.DetailSectionNumber,
                ActorRoleId = RoleClinicalId,
                StandardDurationMinutes = detail.DurationMinutes
            });
        }

        store.AddProcedureAttachment(new ProcedureAttachment
        {
            ProcedureVersionId = versionId,
            AttachmentType = "source_pdf",
            FileName = pdfFileName,
            FileUri = "imported/" + pdfFileName,
            MimeType = "application/pdf",
            FileSizeBytes = fileSize,
            ChecksumSha256 = checksum,
            UploadedBy = AdminUserId
        });
    }

    private sealed record KsnkFlowStepTemplate(
        string Name,
        string Responsibility,
        string Description,
        string FormReference,
        string ShapeCode,
        string DetailSectionNumber,
        int DurationMinutes);

    private static IReadOnlyList<KsnkFlowStepTemplate> KsnkFlowDetails(string code, IReadOnlyList<string> steps)
    {
        if (code == "QT.KSNK.09")
        {
            return
            [
                new("Làm sạch dụng cụ", "ĐD dụng cụ - khoa GMHS", "Sau sử dụng, dụng cụ được đưa về khu vực xử lý riêng; nhân viên mang phương tiện PHCN, pha hóa chất theo khuyến cáo, loại bỏ chất thải còn sót, tháo rời và mở các khớp/góc. Dụng cụ được ngâm ngập trong hóa chất làm sạch chứa enzyme đúng thời gian, chà rửa bằng dụng cụ chuyên dụng, tráng nước sạch và làm khô bằng khăn/gạc sạch hoặc để khô tự nhiên.", "BM.KSNK.09.01\nBM.KSNK.09.02", "terminator", "5.2.1", 10),
                new("Giao nhận dụng cụ sau khi làm sạch", "- ĐD dụng cụ - khoa GMHS\n- NV khu vực làm sạch, khử khuẩn dụng cụ - khoa KSNK", "ĐD dụng cụ kiểm tra dụng cụ đã sạch, khô, không còn máu, mủ, dịch tiết hoặc hóa chất; đặt dụng cụ vào thùng có nắp đậy và vận chuyển an toàn tới nơi nhận dụng cụ bẩn của khoa KSNK. Hai bên kiểm đếm, đối chiếu sổ giao nhận; trường hợp hư hỏng, thất lạc hoặc cần dùng khẩn cấp phải ghi nhận, ký xác nhận và báo điều dưỡng trưởng liên quan.", "BM.KSNK.09.09\nPhụ lục I\nPhụ lục II", "process", "5.2.2", 10),
                new("Làm sạch, khử khuẩn dụng cụ", "NV khu vực làm sạch, khử khuẩn dụng cụ - khoa KSNK", "NV KSNK nhận dụng cụ, kiểm tra độ sạch và số lượng, báo trả khoa sử dụng nếu còn tồn dư bẩn. Dụng cụ được xử lý lại theo nhóm phù hợp bằng phương tiện làm sạch, máy rửa/khử khuẩn hoặc thao tác thủ công, tuân thủ hóa chất, nồng độ, thời gian tiếp xúc, tráng và làm khô trước khi chuyển sang đóng gói.", "BM.KSNK.09.03\nBM.KSNK.09.04", "process", "5.2.3", 10),
                new("Bảo dưỡng - kiểm tra dụng cụ", "NV khu vực đóng gói dụng cụ - khoa KSNK", "Dụng cụ sau làm sạch được bảo dưỡng theo khuyến cáo của nhà sản xuất, kiểm tra bằng mắt thường hoặc kính/đèn phóng đại. Nhân viên kiểm tra khớp, khóa, lòng ống, răng cưa, bề mặt, độ sắc bén, độ khô, tình trạng gỉ sét/hư hỏng và loại bỏ hoặc báo sửa chữa dụng cụ không đạt.", "Phụ lục III\nPhụ lục IV\nPhụ lục V\nPhụ lục VI", "process", "5.2.4", 10),
                new("Đóng gói dụng cụ", "NV khu vực đóng gói dụng cụ - khoa KSNK", "Đóng gói dụng cụ bằng bao túi ép chuyên dụng, hộp hoặc khay theo chủng loại dụng cụ. Bố trí dụng cụ đúng vị trí, đặt chỉ thị hóa học phù hợp, hàn kín, ghi/dán nhãn thông tin lô, ngày đóng gói, người đóng gói, hạn dùng và chuyển sang khu vực tiệt khuẩn.", "BM.KSNK.09.05\nBM.KSNK.09.06\nBM.KSNK.09.07", "process", "5.2.5", 10),
                new("Tiệt khuẩn dụng cụ", "NV vận hành máy hấp - khoa KSNK", "Vận hành máy hấp phù hợp với loại dụng cụ cần tiệt khuẩn:\n- Dụng cụ chịu nhiệt: Máy hấp nhiệt độ cao\n- Dụng cụ không chịu nhiệt: Máy hấp nhiệt độ thấp", "", "process", "", 10),
                new("Giám sát chất lượng tiệt khuẩn dụng cụ", "NV vận hành máy hấp - khoa KSNK", "Theo dõi đầy đủ các thông số chu trình, chỉ thị cơ học, hóa học và sinh học/PCD theo quy định. Kết quả giám sát phải được ghi nhận; mẻ không đạt phải cách ly, xử lý lại và báo người phụ trách trước khi cấp phát.", "BM.KSNK.09.08\nPhụ lục VII\nPhụ lục VIII", "process", "5.2.6", 10),
                new("Lưu trữ dụng cụ", "NV kho vô khuẩn - khoa KSNK", "Dụng cụ đạt yêu cầu sau tiệt khuẩn được lưu tại kho vô khuẩn, bảo đảm nguyên vẹn bao gói, khô sạch, đúng hạn dùng và được sắp xếp theo nguyên tắc nhập trước - xuất trước.", "BM.KSNK.09.11", "process", "5.2.7", 10),
                new("Giao nhận dụng cụ sau khi tiệt khuẩn", "- NV khu vực cấp phát dụng cụ - khoa KSNK\n- ĐD dụng cụ - khoa GMHS", "NV cấp phát kiểm tra tình trạng vô khuẩn, nhãn, hạn dùng và số lượng trước khi giao. ĐD dụng cụ khoa GMHS tiếp nhận, kiểm đếm, ký sổ giao nhận; mọi sai lệch, rách ướt bao gói hoặc dụng cụ quá hạn phải trả lại để xử lý lại.", "BM.KSNK.09.10\nPhụ lục IX\nPhụ lục X", "terminator", "5.2.8", 10)
            ];
        }

        if (code == "QT.KSNK.12")
        {
            return
            [
                new("Làm sạch, khử khuẩn dụng cụ", "NV khoa sử dụng", "Sau sử dụng, dụng cụ được đưa về khu vực xử lý riêng tại khoa sử dụng. Nhân viên mang PHCN, pha hóa chất theo khuyến cáo, loại bỏ chất thải, tháo rời/mở các khớp và xả nước sạch. Dụng cụ được làm sạch bằng hóa chất chứa enzyme, chà rửa, tráng, làm khô; với dụng cụ có ngóc ngách hoặc dính máu, mủ, dịch tiết thì thực hiện thêm bước khử khuẩn mức độ trung bình theo đúng thời gian và nồng độ.", "BM.KSNK.12.01\nBM.KSNK.12.02", "terminator", "5.2.1", 10),
                new("Giao nhận dụng cụ sau khi làm sạch, khử khuẩn", "- NV khoa sử dụng\n- NV khu vực làm sạch, khử khuẩn dụng cụ - khoa KSNK", "Khoa sử dụng kiểm tra dụng cụ sạch, khô, đặt trong hộp/thùng có nắp đậy và ghi rõ số lượng, chủng loại vào sổ giao nhận. Dụng cụ được bàn giao trực tiếp cho khoa KSNK theo thời gian, địa điểm quy định; hai bên kiểm đếm, ký nhận và xử lý ngay các sai lệch.", "BM.KSNK.12.09\nPhụ lục I", "process", "5.2.2", 10),
                new("Làm sạch, khử khuẩn dụng cụ", "NV khu vực làm sạch, khử khuẩn dụng cụ - khoa KSNK", "NV KSNK kiểm tra độ sạch, số lượng, chủng loại và chất lượng dụng cụ đã nhận. Dụng cụ được làm sạch/khử khuẩn lại bằng máy rửa khử khuẩn hoặc bằng tay tùy loại, tuân thủ bảng hướng dẫn hóa chất, thời gian, nồng độ, quy trình tráng và làm khô trước khi đóng gói.", "BM.KSNK.12.03\nBM.KSNK.12.04", "process", "5.2.3", 10),
                new("Bảo dưỡng - kiểm tra dụng cụ", "NV khu vực đóng gói dụng cụ - khoa KSNK", "Thực hiện bảo dưỡng, bôi trơn nếu cần; kiểm tra chức năng, khóa khớp, lòng ống, vết bẩn còn sót, gỉ sét, biến dạng và độ khô. Dụng cụ không đạt được tách riêng để xử lý lại, sửa chữa hoặc loại bỏ theo quy định.", "Phụ lục II\nPhụ lục III", "process", "5.2.4", 10),
                new("Đóng gói dụng cụ", "NV khu vực đóng gói dụng cụ - khoa KSNK", "Đóng gói bằng bao túi ép chuyên dụng, hộp hoặc khay phù hợp; đặt chỉ thị hóa học, sắp xếp dụng cụ đúng nguyên tắc, hàn/niêm kín, ghi nhãn ngày đóng gói, người đóng gói, lô hấp và hạn dùng trước khi chuyển tiệt khuẩn.", "BM.KSNK.12.05\nBM.KSNK.12.06\nBM.KSNK.12.07", "process", "5.2.5", 10),
                new("Tiệt khuẩn dụng cụ", "NV vận hành máy hấp - khoa KSNK", "Vận hành máy hấp phù hợp với loại dụng cụ cần tiệt khuẩn theo khuyến cáo của nhà sản xuất:\n- Dụng cụ chịu nhiệt: Máy hấp nhiệt độ cao\n- Dụng cụ không chịu nhiệt: Máy hấp nhiệt độ thấp", "", "process", "", 10),
                new("Giám sát chất lượng tiệt khuẩn dụng cụ", "NV vận hành máy hấp - khoa KSNK", "Giám sát thông số mẻ hấp, chỉ thị hóa học, chỉ thị sinh học/PCD theo quy định; ghi nhận kết quả và cách ly toàn bộ mẻ nếu không đạt để xử lý lại.", "BM.KSNK.12.08\nPhụ lục IV\nPhụ lục V", "process", "5.2.6", 10),
                new("Lưu trữ dụng cụ", "NV kho vô khuẩn - khoa KSNK", "Lưu dụng cụ đã tiệt khuẩn tại kho vô khuẩn, kiểm tra bao gói nguyên vẹn, nhãn và hạn dùng; sắp xếp tránh ẩm, bụi, đè ép và cấp phát theo nguyên tắc nhập trước - xuất trước.", "BM.KSNK.12.11", "process", "5.2.7", 10),
                new("Giao nhận dụng cụ sau khi tiệt khuẩn", "- NV khu vực cấp phát dụng cụ - khoa KSNK\n- NV khoa sử dụng", "NV cấp phát kiểm tra dụng cụ vô khuẩn trước khi giao; khoa sử dụng kiểm đếm, ký nhận và bảo quản tới khi dùng. Dụng cụ hư bao gói, quá hạn hoặc nghi ngờ nhiễm bẩn phải trả lại khoa KSNK.", "BM.KSNK.12.10\nPhụ lục I", "terminator", "5.2.8", 10)
            ];
        }

        if (code == "QT.KSNK.16")
        {
            return
            [
                new("Làm sạch dụng cụ", "NV khoa sử dụng", "Dụng cụ bán thiết yếu/hỗ trợ hô hấp sau sử dụng được đưa về khu xử lý riêng. Nhân viên mang PHCN, pha hóa chất làm sạch chứa enzyme, loại bỏ chất thải, tháo rời, xả nước, ngâm đúng thời gian/nồng độ, chà rửa bằng phương tiện chuyên dụng, tráng nước sạch và làm khô.", "BM.KSNK.16.01", "terminator", "5.2.1", 10),
                new("Giao nhận dụng cụ sau khi làm sạch", "- NV khoa sử dụng\n- NV khu vực làm sạch, khử khuẩn dụng cụ - khoa KSNK", "Khoa sử dụng kiểm tra dụng cụ sạch, khô; đặt vào hộp có nắp đậy và ghi đầy đủ số lượng, chủng loại vào sổ giao nhận. NV KSNK kiểm đếm, đối chiếu, ký nhận; nếu còn bẩn hoặc sai lệch phải yêu cầu xử lý lại hoặc điều chỉnh sổ ngay tại thời điểm giao nhận.", "BM.KSNK.16.02\nPhụ lục I", "process", "5.2.2", 10),
                new("Khử khuẩn mức độ cao dụng cụ", "NV khu vực làm sạch, khử khuẩn dụng cụ - khoa KSNK", "NV KSNK chuẩn bị bồn/khay ngâm có nắp, que thử nồng độ, đồng hồ đếm ngược, khăn vô khuẩn, máy sấy và hóa chất KKMĐC còn hạn. Kiểm tra nồng độ hóa chất từ mẻ đầu và đầu mỗi ngày, ngâm dụng cụ ngập hoàn toàn theo thời gian quy định, tráng bằng nước vô khuẩn/đạt yêu cầu, làm khô và chuyển đóng gói vô khuẩn.", "BM.KSNK.16.03\nPhụ lục II\nPhụ lục III", "process", "5.2.3", 10),
                new("Đóng gói dụng cụ", "NV khu vực đóng gói dụng cụ - khoa KSNK", "- Mang phương tiện PHCN: nón, khẩu trang\n- Vệ sinh tay\n- Trải khăn vô khuẩn lên bàn đóng gói dụng cụ KKMĐC\n- Mang áo choàng vô khuẩn, găng vô khuẩn\n- Lấy dụng cụ ra từ tủ sấy và kiểm tra độ khô; để dụng cụ lên bàn đã trải khăn vô khuẩn\n- Đóng gói dụng cụ bằng bao túi ép chuyên dụng đã được hàn một đầu và hấp tiệt khuẩn\n- Đóng dấu hoặc dán nhãn thông tin: ngày đóng gói, nhân viên đóng gói, hạn sử dụng 14 ngày\n- Chuyển dụng cụ qua kho vô khuẩn bằng hộp trung chuyển (Passbox)", "BM.KSNK.16.04", "process", "", 10),
                new("Lưu trữ dụng cụ tại khoa KSNK", "NV kho vô khuẩn - khoa KSNK", "Lưu trữ dụng cụ KKMĐC đã đóng gói tại kho vô khuẩn, duy trì bao gói khô, sạch, nguyên vẹn và đúng hạn sử dụng 14 ngày đến khi bàn giao.", "", "process", "", 10),
                new("Giao nhận dụng cụ vô khuẩn", "- NV khu vực cấp phát dụng cụ - khoa KSNK\n- NV khoa sử dụng", "NV kho cấp phát kiểm tra bao gói, hạn dùng và số lượng; khoa sử dụng tiếp nhận, ký sổ giao nhận và bảo quản dụng cụ vô khuẩn theo quy định trước khi dùng.", "BM.KSNK.16.05", "terminator", "5.2.4", 10)
            ];
        }

        if (code == "QT.KSNK.17")
        {
            return
            [
                new("Chuẩn bị", "NV khoa sử dụng", "Nhân viên chuẩn bị khăn sạch, bàn chải, hóa chất khử khuẩn bề mặt, dầu bôi trơn chuyên dụng cho tay khoan và mang PHCN đúng quy định gồm trùm tóc, kính hoặc mạng che mặt, khẩu trang, tạp dề và găng tay.", "", "terminator", "5.2.2", 10),
                new("Làm sạch", "NV khoa sử dụng", "Sau khi sử dụng trong miệng người bệnh, tay khoan được cho chạy không tải 10 - 15 giây để loại bỏ nước bọt, máu đọng trong lòng tay khoan; tháo mũi khoan, cọ rửa dưới vòi nước chảy, không ngâm ngập trong nước, làm khô bên ngoài bằng khăn sạch và làm khô bên trong bằng hơi 10 - 15 giây đối với tay khoan tốc độ cao.", "", "process", "5.2.3", 10),
                new("Khử khuẩn", "NV khoa sử dụng", "Lau bên ngoài tay khoan bằng khăn/giấy thấm hóa chất khử khuẩn bề mặt phù hợp và tuân thủ thời gian tiếp xúc theo khuyến cáo. Không ngâm tay khoan trong hóa chất; sau khử khuẩn xả lại dưới vòi nước chảy, làm khô bên ngoài và thổi khô bên trong 10 - 15 giây với tay khoan tốc độ cao.", "", "process", "5.2.4", 10),
                new("Tra dầu bôi trơn", "NV khoa sử dụng", "Tra dầu bôi trơn theo hướng dẫn của nhà sản xuất và cho chạy nhẹ trong 10 - 15 giây với dầu bôi trơn.", "", "process", "", 10),
                new("Giao nhận dụng cụ sau khi làm sạch, khử khuẩn", "- NV khoa sử dụng\n- NV khoa KSNK", "Khoa sử dụng chuyển tay khoan đã làm sạch/khử khuẩn cho khoa KSNK, ghi số lượng và tình trạng vào sổ giao nhận. Hai bên kiểm đếm, ký xác nhận và xử lý ngay sai lệch hoặc dụng cụ hư hỏng.", "", "process", "5.2.5", 10),
                new("Đóng gói", "NV khoa KSNK", "NV khoa KSNK kiểm tra tay khoan khô, sạch, tra dầu/bảo dưỡng theo hướng dẫn nhà sản xuất nếu cần, đóng gói bằng vật liệu phù hợp và ghi nhãn trước tiệt khuẩn.", "", "process", "5.2.6", 10),
                new("Tiệt khuẩn", "NV khoa KSNK", "Tiệt khuẩn tay khoan theo hướng dẫn của nhà sản xuất, lựa chọn phương pháp và chu trình phù hợp với cấu tạo tay khoan; ghi nhận mẻ tiệt khuẩn và kết quả giám sát.", "", "process", "", 10),
                new("Lưu trữ tại khoa KSNK", "NV khoa KSNK", "Dụng cụ sau tiệt khuẩn được lưu trữ tại kho vô khuẩn, bảo đảm bao gói nguyên vẹn, khô sạch, đúng nhãn và còn hạn sử dụng.", "", "process", "", 10),
                new("Giao nhận dụng cụ sau khi tiệt khuẩn", "- NV khoa KSNK\n- NV khoa sử dụng", "Khoa KSNK kiểm tra số lượng, tình trạng vô khuẩn và bàn giao tay khoan cho khoa sử dụng; khoa sử dụng ký nhận và bảo quản đến khi sử dụng cho người bệnh.", "", "terminator", "5.2.7", 10)
            ];
        }

        return steps.Select((step, index) => new KsnkFlowStepTemplate(
            step,
            DefaultKsnkResponsibility(step, index, steps.Count),
            DefaultKsnkStepDescription(step, index),
            "Biểu mẫu/phụ lục: đối chiếu theo PDF scan nguồn.",
            index == 0 || index == steps.Count - 1 ? "terminator" : "process",
            $"5.2.{index + 1}",
            10)).ToList();
    }

    private static string DefaultKsnkResponsibility(string step, int index, int total)
    {
        if (index == 0) return "Khoa sử dụng / Khoa KSNK";
        if (index == total - 1) return "Khoa KSNK / Khoa sử dụng";
        if (step.Contains("tiệt khuẩn", StringComparison.OrdinalIgnoreCase) ||
            step.Contains("Giám sát", StringComparison.OrdinalIgnoreCase))
            return "NV vận hành máy hấp - khoa KSNK";
        if (step.Contains("Lưu trữ", StringComparison.OrdinalIgnoreCase))
            return "NV kho vô khuẩn - khoa KSNK";
        if (step.Contains("Đóng gói", StringComparison.OrdinalIgnoreCase) ||
            step.Contains("Bảo dưỡng", StringComparison.OrdinalIgnoreCase))
            return "NV khu vực đóng gói dụng cụ - khoa KSNK";
        return "NV khu vực làm sạch, khử khuẩn dụng cụ - khoa KSNK";
    }

    private static string DefaultKsnkStepDescription(string step, int index)
        => $"{index + 1}. {step}: thực hiện theo diễn giải chi tiết trong PDF scan nguồn; ghi nhận hồ sơ và biểu mẫu tương ứng trước khi chuyển bước tiếp theo.";

    private static string KsnkPurpose(string code) => code switch
    {
        "QT.KSNK.09" => "Thống nhất quy trình xử lý dụng cụ phẫu thuật; tăng cường thực hành tốt xử lý dụng cụ, hạn chế thấp nhất nguy cơ nhiễm khuẩn, bảo đảm an toàn người bệnh và chất lượng phẫu thuật.",
        "QT.KSNK.12" => "Thống nhất quy trình xử lý dụng cụ y tế nhằm cung cấp đầy đủ và duy trì chất lượng khử khuẩn, tiệt khuẩn cho dụng cụ y tế sử dụng lại trong bệnh viện, đáp ứng yêu cầu an toàn người bệnh và nâng cao chất lượng khám chữa bệnh.",
        "QT.KSNK.16" => "Thống nhất quy trình khử khuẩn mức độ cao nhằm cung cấp đầy đủ và duy trì chất lượng khử khuẩn cho dụng cụ y tế sử dụng lại trong bệnh viện, đáp ứng yêu cầu an toàn người bệnh và nâng cao chất lượng điều trị.",
        "QT.KSNK.17" => "Tiệt khuẩn tay khoan nha khoa nhằm kiểm soát, phòng chống lây nhiễm chéo cho người bệnh trong thực hiện thủ thuật, đáp ứng yêu cầu an toàn người bệnh và nâng cao chất lượng điều trị.",
        _ => "Thống nhất quy trình xử lý dụng cụ y tế theo yêu cầu kiểm soát nhiễm khuẩn của bệnh viện."
    };

    private static string KsnkScope(string code) => code switch
    {
        "QT.KSNK.09" => "Áp dụng cho khoa Gây mê hồi sức và khoa Kiểm soát nhiễm khuẩn trong tiếp nhận, xử lý, tiệt khuẩn, lưu trữ và bàn giao dụng cụ phẫu thuật phục vụ phẫu thuật tại Bệnh viện Ung Bướu.",
        "QT.KSNK.12" => "Áp dụng cho các khoa lâm sàng, cận lâm sàng đang quản lý dụng cụ y tế gửi khoa Kiểm soát nhiễm khuẩn để xử lý tập trung; áp dụng cho nhân viên các khoa liên quan trong tiếp nhận, xử lý và bàn giao dụng cụ y tế.",
        "QT.KSNK.16" => "Áp dụng đối với dụng cụ bán thiết yếu và dụng cụ hỗ trợ hô hấp không thể tiệt khuẩn; áp dụng cho nhân viên các khoa lâm sàng, cận lâm sàng và khoa Kiểm soát nhiễm khuẩn được giao nhiệm vụ xử lý dụng cụ.",
        "QT.KSNK.17" => "Áp dụng cho bác sĩ, trợ thủ nha khoa, nhân viên phòng khám răng miệng và nhân viên khoa Kiểm soát nhiễm khuẩn trong quá trình xử lý tay khoan nha khoa tại Bệnh viện Ung Bướu.",
        _ => "Áp dụng cho các khoa/phòng sử dụng dụng cụ và khoa Kiểm soát nhiễm khuẩn."
    };

    private static string KsnkBasis(string code)
    {
        var common = "Quyết định 3671/QĐ-BYT ngày 27/9/2012 của Bộ Y tế về Hướng dẫn khử khuẩn, tiệt khuẩn dụng cụ trong các cơ sở khám bệnh, chữa bệnh.\nThông tư 16/2018/TT-BYT ngày 20/7/2018 của Bộ Y tế quy định về kiểm soát nhiễm khuẩn trong các cơ sở khám bệnh, chữa bệnh.";
        return code switch
        {
            "QT.KSNK.09" => common + "\nQuyết định 3916/QĐ-BYT ngày 28/8/2017 của Bộ Y tế về Hướng dẫn xử lý dụng cụ phẫu thuật nội soi trong các cơ sở khám bệnh, chữa bệnh.",
            "QT.KSNK.17" => "Quyết định 3671/QĐ-BYT ngày 27/9/2012 của Bộ Y tế về Hướng dẫn khử khuẩn, tiệt khuẩn dụng cụ trong các cơ sở khám bệnh, chữa bệnh.\nQuyết định 5991/QĐ-BYT ngày 26/12/2019 của Bộ Y tế về Hướng dẫn kiểm soát nhiễm khuẩn trong khám bệnh, chữa bệnh răng miệng.",
            _ => common
        };
    }

    private static string KsnkDefinitions(string code) => code switch
    {
        "QT.KSNK.16" => "Dụng cụ bán thiết yếu: dụng cụ tiếp xúc với niêm mạc hoặc da bị tổn thương.\nDụng cụ hỗ trợ hô hấp: dụng cụ sử dụng để hỗ trợ quá trình hô hấp của người bệnh hoặc thực hiện kỹ thuật chăm sóc, điều trị liên quan đến đường hô hấp.\nKhử khuẩn mức độ cao: quá trình tiêu diệt toàn bộ vi sinh vật và một số bào tử vi khuẩn.\nLàm sạch: quá trình dùng biện pháp cơ học và hóa học để loại bỏ tác nhân nhiễm khuẩn và chất hữu cơ bám trên dụng cụ trước khi khử khuẩn/tiệt khuẩn.\nTừ viết tắt: LS - làm sạch; KKMĐC - khử khuẩn mức độ cao; KSNK - kiểm soát nhiễm khuẩn; NV - nhân viên; NSX - nhà sản xuất; PHCN - phòng hộ cá nhân.",
        "QT.KSNK.17" => "Tay khoan nha khoa: dụng cụ cơ học cầm tay dùng trong thủ thuật nha khoa, gồm các bộ phận cơ học tạo lực quay và cấp lực cho dụng cụ cắt.\nTay khoan tốc độ cao: tay khoan hoạt động trên 180.000 vòng/phút. Tay khoan tốc độ chậm: tay khoan hoạt động từ 600 đến 25.000 vòng/phút.\nTiệt khuẩn: quá trình tiêu diệt hoặc loại bỏ tất cả dạng vi sinh vật sống, bao gồm bào tử vi khuẩn.\nKhử khuẩn: quá trình loại bỏ hầu hết hoặc tất cả vi sinh vật gây bệnh trên dụng cụ nhưng không diệt bào tử vi khuẩn.\nLàm sạch/khử nhiễm: quá trình dùng biện pháp cơ học và hóa học để loại bỏ tác nhân nhiễm khuẩn và chất hữu cơ bám trên dụng cụ trước khử khuẩn/tiệt khuẩn.\nTừ viết tắt: ĐD - điều dưỡng; NV - nhân viên; NVYT - nhân viên y tế; NB - người bệnh; KSNK - kiểm soát nhiễm khuẩn; TKTT - tiệt khuẩn trung tâm; DC - dụng cụ; HC - hóa chất; PHCN - phòng hộ cá nhân.",
        _ => "Tiệt khuẩn: quá trình tiêu diệt hoặc loại bỏ tất cả dạng vi sinh vật sống, bao gồm bào tử vi khuẩn.\nKhử khuẩn: quá trình loại bỏ hầu hết hoặc tất cả vi sinh vật gây bệnh trên dụng cụ nhưng không diệt bào tử vi khuẩn; gồm mức độ thấp, trung bình và cao.\nKhử khuẩn mức độ cao: quá trình tiêu diệt toàn bộ vi sinh vật và một số bào tử vi khuẩn.\nLàm sạch: quá trình dùng biện pháp cơ học và hóa học để loại bỏ tác nhân nhiễm khuẩn và chất hữu cơ bám trên dụng cụ; là bước bắt buộc trước khử khuẩn/tiệt khuẩn.\nTừ viết tắt: LS - làm sạch; KK - khử khuẩn; TK - tiệt khuẩn; KSNK - kiểm soát nhiễm khuẩn; NV - nhân viên; PHCN - phòng hộ cá nhân; ĐD - điều dưỡng; VT,TBYT - vật tư, thiết bị y tế; ĐDT - điều dưỡng trưởng."
    };

    private static string KsnkProcedureNarrative(string code) => string.Join("\n\n",
        KsnkFlowDetails(code, code switch
        {
            "QT.KSNK.09" => ["Làm sạch dụng cụ", "Giao nhận dụng cụ sau khi làm sạch", "Làm sạch, khử khuẩn dụng cụ", "Bảo dưỡng - kiểm tra dụng cụ", "Đóng gói dụng cụ", "Tiệt khuẩn dụng cụ", "Giám sát chất lượng tiệt khuẩn dụng cụ", "Lưu trữ dụng cụ", "Giao nhận dụng cụ sau khi tiệt khuẩn"],
            "QT.KSNK.12" => ["Làm sạch, khử khuẩn dụng cụ", "Giao nhận dụng cụ sau khi làm sạch", "Làm sạch, khử khuẩn dụng cụ", "Bảo dưỡng - kiểm tra dụng cụ", "Đóng gói dụng cụ", "Tiệt khuẩn dụng cụ", "Giám sát chất lượng tiệt khuẩn dụng cụ", "Lưu trữ dụng cụ", "Giao nhận dụng cụ sau khi tiệt khuẩn"],
            "QT.KSNK.16" => ["Làm sạch dụng cụ", "Giao nhận dụng cụ sau khi làm sạch", "Khử khuẩn mức độ cao dụng cụ", "Đóng gói dụng cụ", "Lưu trữ dụng cụ tại khoa KSNK", "Giao nhận dụng cụ vô khuẩn"],
            "QT.KSNK.17" => ["Chuẩn bị", "Làm sạch", "Khử khuẩn", "Tra dầu bôi trơn", "Giao nhận dụng cụ sau khi làm sạch, khử khuẩn", "Đóng gói", "Tiệt khuẩn", "Lưu trữ tại khoa KSNK", "Giao nhận dụng cụ sau khi tiệt khuẩn"],
            _ => []
        }).Select(step => $"{StepSectionLabel(step)} {step.Name}\nTrách nhiệm: {step.Responsibility}\nDiễn giải: {step.Description}\nBiểu mẫu/phụ lục: {(string.IsNullOrWhiteSpace(step.FormReference) ? "Theo hồ sơ kiểm soát của quy trình." : step.FormReference.Replace("\n", "; "))}"));

    private static string StepSectionLabel(KsnkFlowStepTemplate step)
        => string.IsNullOrWhiteSpace(step.DetailSectionNumber) ? "5.2." : $"{step.DetailSectionNumber}.";

    private static string KsnkRecords(string code) => code switch
    {
        "QT.KSNK.09" => "BM.KSNK.09.01 Bảng kiểm chuẩn bị phương tiện làm sạch dụng cụ phẫu thuật.\nBM.KSNK.09.02 Bảng kiểm làm sạch dụng cụ phẫu thuật.\nBM.KSNK.09.03 - BM.KSNK.09.04 Bảng kiểm làm sạch, khử khuẩn dụng cụ tại khoa KSNK.\nBM.KSNK.09.05 - BM.KSNK.09.07 Bảng kiểm đóng gói dụng cụ.\nBM.KSNK.09.08 Bảng kiểm giám sát chất lượng tiệt khuẩn.\nBM.KSNK.09.09 Bảng kiểm giao nhận dụng cụ sau làm sạch.\nBM.KSNK.09.10 Bảng kiểm giao nhận dụng cụ sau tiệt khuẩn.\nBM.KSNK.09.11 Bảng kiểm lưu trữ dụng cụ.\nPhụ lục I-X theo PDF nguồn.",
        "QT.KSNK.12" => "BM.KSNK.12.01 - BM.KSNK.12.02 Bảng kiểm chuẩn bị, làm sạch và khử khuẩn dụng cụ tại khoa sử dụng.\nBM.KSNK.12.03 - BM.KSNK.12.04 Bảng kiểm làm sạch/khử khuẩn dụng cụ tại CSSD.\nBM.KSNK.12.05 - BM.KSNK.12.07 Bảng kiểm đóng gói dụng cụ.\nBM.KSNK.12.08 Bảng kiểm giám sát chất lượng tiệt khuẩn.\nBM.KSNK.12.09 - BM.KSNK.12.10 Bảng kiểm giao nhận dụng cụ trước và sau tiệt khuẩn.\nBM.KSNK.12.11 Bảng kiểm lưu trữ dụng cụ.\nPhụ lục I-V theo PDF nguồn.",
        "QT.KSNK.16" => "BM.KSNK.16.01 Bảng kiểm làm sạch dụng cụ.\nBM.KSNK.16.02 Bảng kiểm giao nhận dụng cụ sau làm sạch.\nBM.KSNK.16.03 Bảng kiểm khử khuẩn mức độ cao.\nBM.KSNK.16.04 Bảng kiểm đóng gói dụng cụ KKMĐC.\nBM.KSNK.16.05 Bảng kiểm giao nhận dụng cụ vô khuẩn.\nPhụ lục I-III theo PDF nguồn.",
        "QT.KSNK.17" => "BM.KSNK.17.01 Bảng kiểm xử lý tay khoan nha khoa.\nPhụ lục I Sổ giao nhận dụng cụ y tế.",
        _ => "Hồ sơ, biểu mẫu và phụ lục theo PDF nguồn đính kèm."
    };

    private static void AddDefaultProcedureSections(MedDataStore store, Guid versionId, string code)
    {
        (string number, string title, string kind, string text)[] sections =
        [
            ("I", "Mục đích", "purpose", KsnkPurpose(code)),
            ("II", "Phạm vi áp dụng", "scope", KsnkScope(code)),
            ("III", "Căn cứ và tài liệu viện dẫn", "basis", KsnkBasis(code)),
            ("IV", "Thuật ngữ và định nghĩa", "definitions", KsnkDefinitions(code)),
            ("V", "Trách nhiệm", "responsibilities", "Người viết, người kiểm tra, người phê duyệt và các khoa/phòng liên quan chịu trách nhiệm theo bảng ký duyệt, bảng phân phối và từng bước trong lưu đồ."),
            ("VI", "Nơi nhận và phân phối", "distribution", "Xem bảng Nơi nhận trên bìa quy trình."),
            ("VII", "Theo dõi sửa đổi", "revision", "Xem bảng Theo dõi sửa đổi trên bìa quy trình."),
            ("VIII", "Nội dung quy trình", "procedure", KsnkProcedureNarrative(code)),
            ("IX", "Lưu đồ", "flowchart", "Lưu đồ được trình bày tại trang lưu đồ của bản in, theo bảng ba cột Trách nhiệm - Các bước thực hiện - Mô tả/Các biểu mẫu."),
            ("X", "Hồ sơ, biểu mẫu và phụ lục", "records", KsnkRecords(code)),
            ("XI", "Tệp đính kèm", "appendices", "PDF scan nguồn được gắn kèm với checksum SHA-256.")
        ];

        for (var i = 0; i < sections.Length; i++)
        {
            store.AddProcedureDocumentSection(new ProcedureDocumentSection
            {
                ProcedureVersionId = versionId,
                SectionOrder = i + 1,
                SectionNumber = sections[i].number,
                Title = sections[i].title,
                SectionKind = sections[i].kind,
                ContentText = sections[i].text
            });
        }
    }

    private static void SeedTechnicalServices(MedDataStore store)
    {
        // Dịch vụ kỹ thuật: Xét nghiệm công thức máu
        store.AddTechnicalService(new TechnicalService
        {
            TechnicalServiceId = SvcXnCtmId,
            ServiceCode = "DV-XN-CTM",
            Name = "Xét nghiệm công thức máu",
            ServiceType = "lab",
            DepartmentId = DeptXetNghiemId,
            Description = "Xét nghiệm công thức máu toàn phần (CBC)",
            CreatedBy = BacSiXnId
        });

        // Nguồn lực: Ống nghiệm EDTA
        store.AddResourceCatalogItem(new ResourceCatalogItem
        {
            ResourceId = ResOngEdtaId,
            ResourceType = "supply",
            ResourceCode = "VT-ONG-EDTA",
            Name = "Ống nghiệm EDTA",
            DefaultUnitCode = "ampoule"
        });

        // Nguồn lực: Kim lấy máu
        store.AddResourceCatalogItem(new ResourceCatalogItem
        {
            ResourceId = ResKimLayMauId,
            ResourceType = "supply",
            ResourceCode = "VT-KIM-LAY-MAU",
            Name = "Kim lấy máu",
            DefaultUnitCode = "piece"
        });

        // Định mức: 2 ống EDTA cho mỗi lần xét nghiệm CTM
        store.AddTechnicalResourceNorm(new TechnicalResourceNorm
        {
            TechnicalServiceId = SvcXnCtmId,
            ResourceId = ResOngEdtaId,
            StandardQuantity = 2,
            UnitCode = "ampoule",
            IsRequired = true,
            Note = "Mỗi lần xét nghiệm cần 2 ống EDTA"
        });

        // Định mức: 1 kim lấy máu cho mỗi lần xét nghiệm CTM
        store.AddTechnicalResourceNorm(new TechnicalResourceNorm
        {
            TechnicalServiceId = SvcXnCtmId,
            ResourceId = ResKimLayMauId,
            StandardQuantity = 1,
            UnitCode = "piece",
            IsRequired = true,
            Note = "Mỗi lần lấy máu cần 1 kim"
        });
    }

    private static void SeedClinicalProtocol(MedDataStore store)
    {
        // Phác đồ điều trị tăng huyết áp
        store.AddClinicalProtocol(new ClinicalProtocol
        {
            ClinicalProtocolId = ProtocolThaId,
            ProtocolCode = "PD-NOI-THA",
            Name = "Phác đồ điều trị tăng huyết áp",
            ProtocolType = "treatment_protocol",
            OwnerDepartmentId = DeptNoiId,
            Description = "Phác đồ chuẩn điều trị tăng huyết áp theo khuyến cáo",
            CreatedBy = TruongKhoaNoiId
        });

        store.AddClinicalProtocolVersion(new ClinicalProtocolVersion
        {
            ClinicalProtocolVersionId = ProtocolThaVersionId,
            ClinicalProtocolId = ProtocolThaId,
            VersionNo = 1,
            StatusCode = "active",
            Title = "Phác đồ điều trị tăng huyết áp - Phiên bản 1",
            Summary = "Áp dụng cho bệnh nhân tăng huyết áp nguyên phát",
            CreatedBy = TruongKhoaNoiId,
            ApprovedBy = AdminUserId,
            PublishedBy = AdminUserId,
            ApprovedAt = DateTime.UtcNow.AddDays(-10),
            PublishedAt = DateTime.UtcNow.AddDays(-10),
            EffectiveFrom = DateTime.UtcNow.AddDays(-10)
        });

        // Quy tắc áp dụng: mã ICD I10-I15 (tăng huyết áp)
        store.AddProtocolApplicabilityRule(new ProtocolApplicabilityRule
        {
            ClinicalProtocolVersionId = ProtocolThaVersionId,
            RuleType = "icd",
            RuleJson = "{\"icdFrom\":\"I10\",\"icdTo\":\"I15\",\"description\":\"Tăng huyết áp nguyên phát và thứ phát\"}",
            Priority = 100,
            IsActive = true
        });
    }

    private static void SeedPatientAndEncounter(MedDataStore store)
    {
        // Bệnh nhân mẫu
        store.AddPatientRef(new PatientRef
        {
            PatientRefId = PatientMauId,
            ExternalPatientId = "BN-2024-001",
            PatientCode = "BN-2024-001",
            DisplayName = "Bệnh nhân mẫu",
            BirthDate = new DateOnly(1975, 3, 15),
            GenderCode = "male"
        });

        // Lượt khám ngoại trú tại Khoa Nội
        store.AddEncounterRef(new EncounterRef
        {
            EncounterRefId = EncounterMauId,
            PatientRefId = PatientMauId,
            ExternalEncounterId = "LK-2024-001",
            EncounterType = "outpatient",
            DepartmentId = DeptNoiId,
            StartedAt = DateTime.UtcNow.AddHours(-2)
        });
    }

    private static void SeedTechnicalOrder(MedDataStore store)
    {
        // Chỉ định xét nghiệm CTM - đã hoàn thành
        store.AddTechnicalOrder(new TechnicalOrder
        {
            TechnicalOrderId = OrderCtmId,
            TechnicalServiceId = SvcXnCtmId,
            PatientRefId = PatientMauId,
            EncounterRefId = EncounterMauId,
            OrderingDepartmentId = DeptNoiId,
            OrderedBy = BacSiNoiId,
            OrderStatus = "completed",
            OrderedAt = DateTime.UtcNow.AddHours(-1),
            CompletedAt = DateTime.UtcNow.AddMinutes(-30)
        });

        // Sử dụng nguồn lực thực tế: 2 ống EDTA (IsFinal = true)
        store.AddActualResourceUsage(new ActualResourceUsage
        {
            TechnicalOrderId = OrderCtmId,
            ResourceId = ResOngEdtaId,
            ActualQuantity = 2,
            UnitCode = "ampoule",
            IsFinal = true,
            CapturedBy = BacSiXnId
        });
    }

    private static void SeedNotifications(MedDataStore store)
    {
        // Thông báo cho admin: quy trình mới chờ phê duyệt
        store.AddNotification(new MedNotification
        {
            RecipientUserId = AdminUserId,
            NotificationType = "procedure_approval",
            Title = "Quy trình mới chờ phê duyệt",
            Body = "Quy trình 'Xét nghiệm công thức máu' đã được gửi để phê duyệt.",
            Severity = "info",
            SourceType = "procedure_version",
            SourceId = ProcXnVersionId.ToString()
        });

        // Thông báo cho Trưởng khoa Nội: phiên bản quy trình đã được gửi duyệt
        store.AddNotification(new MedNotification
        {
            RecipientUserId = TruongKhoaNoiId,
            NotificationType = "procedure_submitted",
            Title = "Phiên bản quy trình đã được gửi duyệt",
            Body = "Phiên bản 1 của quy trình 'Khám bệnh nội khoa' đã được phê duyệt và xuất bản.",
            Severity = "info",
            SourceType = "procedure_version",
            SourceId = ProcNoiVersionId.ToString()
        });
    }
}
