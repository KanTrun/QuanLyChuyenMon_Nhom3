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
            PrimaryDepartmentId = RootDeptId
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
