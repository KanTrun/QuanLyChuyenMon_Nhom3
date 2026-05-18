using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>
/// Dữ liệu mẫu thực tế cho hệ thống QLCM (Quản Lý Chuyên Môn).
/// Được gọi từ constructor của MedDataStore để khởi tạo dữ liệu demo.
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
        SeedClinicalProtocols(store);
        SeedNotifications(store);
    }

    // === IDs cố định để tham chiếu chéo ===
    public static readonly Guid RootDeptId = Guid.Parse("a0000000-0000-0000-0000-000000000001");
    public static readonly Guid DeptNoiId = Guid.Parse("a0000000-0000-0000-0000-000000000010");
    public static readonly Guid DeptNgoaiId = Guid.Parse("a0000000-0000-0000-0000-000000000020");
    public static readonly Guid DeptSanId = Guid.Parse("a0000000-0000-0000-0000-000000000030");
    public static readonly Guid DeptNhiId = Guid.Parse("a0000000-0000-0000-0000-000000000040");
    public static readonly Guid DeptXetNghiemId = Guid.Parse("a0000000-0000-0000-0000-000000000050");
    public static readonly Guid DeptCdhaId = Guid.Parse("a0000000-0000-0000-0000-000000000060");
    public static readonly Guid DeptHcId = Guid.Parse("a0000000-0000-0000-0000-000000000070");

    public static readonly Guid RoleSysAdminId = Guid.Parse("b0000000-0000-0000-0000-000000000001");
    public static readonly Guid RoleDeptAdminId = Guid.Parse("b0000000-0000-0000-0000-000000000002");
    public static readonly Guid RoleClinicalId = Guid.Parse("b0000000-0000-0000-0000-000000000003");
    public static readonly Guid RoleReportId = Guid.Parse("b0000000-0000-0000-0000-000000000004");
    public static readonly Guid RoleNurseId = Guid.Parse("b0000000-0000-0000-0000-000000000005");

    public static readonly Guid UserAnId = Guid.Parse("c0000000-0000-0000-0000-000000000001");
    public static readonly Guid UserBinhId = Guid.Parse("c0000000-0000-0000-0000-000000000002");
    public static readonly Guid UserCuongId = Guid.Parse("c0000000-0000-0000-0000-000000000003");
    public static readonly Guid UserDungId = Guid.Parse("c0000000-0000-0000-0000-000000000004");
    public static readonly Guid UserEmId = Guid.Parse("c0000000-0000-0000-0000-000000000005");
    public static readonly Guid UserPhucId = Guid.Parse("c0000000-0000-0000-0000-000000000006");
    public static readonly Guid UserGiangId = Guid.Parse("c0000000-0000-0000-0000-000000000007");
    public static readonly Guid UserHanhId = Guid.Parse("c0000000-0000-0000-0000-000000000008");
    public static readonly Guid UserKhanhId = Guid.Parse("c0000000-0000-0000-0000-000000000009");
    public static readonly Guid UserLinhId = Guid.Parse("c0000000-0000-0000-0000-000000000010");
    public static readonly Guid UserMinhId = Guid.Parse("c0000000-0000-0000-0000-000000000011");
    public static readonly Guid UserNgocId = Guid.Parse("c0000000-0000-0000-0000-000000000012");

    public static readonly Guid ScreenDashId = Guid.Parse("d0000000-0000-0000-0000-000000000001");
    public static readonly Guid ScreenProcId = Guid.Parse("d0000000-0000-0000-0000-000000000002");
    public static readonly Guid ScreenPermId = Guid.Parse("d0000000-0000-0000-0000-000000000003");
    public static readonly Guid ScreenReportId = Guid.Parse("d0000000-0000-0000-0000-000000000004");
    public static readonly Guid ScreenOrderId = Guid.Parse("d0000000-0000-0000-0000-000000000005");

    public static readonly Guid PermViewDashId = Guid.Parse("e0000000-0000-0000-0000-000000000001");
    public static readonly Guid PermManageProcId = Guid.Parse("e0000000-0000-0000-0000-000000000002");
    public static readonly Guid PermApproveProcId = Guid.Parse("e0000000-0000-0000-0000-000000000003");
    public static readonly Guid PermManagePermId = Guid.Parse("e0000000-0000-0000-0000-000000000004");
    public static readonly Guid PermViewReportId = Guid.Parse("e0000000-0000-0000-0000-000000000005");
    public static readonly Guid PermCreateOrderId = Guid.Parse("e0000000-0000-0000-0000-000000000006");

    private static void SeedDepartments(MedDataStore store)
    {
        store.AddDepartment(new Department
        {
            DepartmentId = RootDeptId,
            Code = "BV-ROOT",
            Name = "Bệnh viện Đa khoa Trung ương",
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
        store.AddUser(new AppUser { UserId = UserAnId, Username = "nguyenvanan", FullName = "Nguyễn Văn An", Email = "an.nguyen@bv.vn", PrimaryDepartmentId = RootDeptId });
        store.AddUser(new AppUser { UserId = UserBinhId, Username = "tranthibinh", FullName = "Trần Thị Bình", Email = "binh.tran@bv.vn", PrimaryDepartmentId = DeptNoiId });
        store.AddUser(new AppUser { UserId = UserCuongId, Username = "levancuong", FullName = "Lê Văn Cường", Email = "cuong.le@bv.vn", PrimaryDepartmentId = DeptNgoaiId });
        store.AddUser(new AppUser { UserId = UserDungId, Username = "phamthidung", FullName = "Phạm Thị Dung", Email = "dung.pham@bv.vn", PrimaryDepartmentId = DeptSanId });
        store.AddUser(new AppUser { UserId = UserEmId, Username = "hoangvanem", FullName = "Hoàng Văn Em", Email = "em.hoang@bv.vn", PrimaryDepartmentId = DeptNhiId });
        store.AddUser(new AppUser { UserId = UserPhucId, Username = "ngothiphuc", FullName = "Ngô Thị Phúc", Email = "phuc.ngo@bv.vn", PrimaryDepartmentId = DeptXetNghiemId });
        store.AddUser(new AppUser { UserId = UserGiangId, Username = "dovangiang", FullName = "Đỗ Văn Giang", Email = "giang.do@bv.vn", PrimaryDepartmentId = DeptCdhaId });
        store.AddUser(new AppUser { UserId = UserHanhId, Username = "vuthihanh", FullName = "Vũ Thị Hạnh", Email = "hanh.vu@bv.vn", PrimaryDepartmentId = DeptHcId });
        store.AddUser(new AppUser { UserId = UserKhanhId, Username = "dangvankhanh", FullName = "Đặng Văn Khánh", Email = "khanh.dang@bv.vn", PrimaryDepartmentId = DeptNoiId });
        store.AddUser(new AppUser { UserId = UserLinhId, Username = "buithilinh", FullName = "Bùi Thị Linh", Email = "linh.bui@bv.vn", PrimaryDepartmentId = DeptNgoaiId });
        store.AddUser(new AppUser { UserId = UserMinhId, Username = "trinhvanminh", FullName = "Trịnh Văn Minh", Email = "minh.trinh@bv.vn", PrimaryDepartmentId = DeptNhiId });
        store.AddUser(new AppUser { UserId = UserNgocId, Username = "luuthingoc", FullName = "Lưu Thị Ngọc", Email = "ngoc.luu@bv.vn", PrimaryDepartmentId = DeptXetNghiemId });

        // Gán vai trò cho người dùng
        store.AddUserRole(new UserRole { UserId = UserAnId, RoleId = RoleSysAdminId });
        store.AddUserRole(new UserRole { UserId = UserBinhId, RoleId = RoleDeptAdminId, DepartmentId = DeptNoiId });
        store.AddUserRole(new UserRole { UserId = UserCuongId, RoleId = RoleClinicalId, DepartmentId = DeptNgoaiId });
        store.AddUserRole(new UserRole { UserId = UserDungId, RoleId = RoleClinicalId, DepartmentId = DeptSanId });
        store.AddUserRole(new UserRole { UserId = UserEmId, RoleId = RoleClinicalId, DepartmentId = DeptNhiId });
        store.AddUserRole(new UserRole { UserId = UserPhucId, RoleId = RoleClinicalId, DepartmentId = DeptXetNghiemId });
        store.AddUserRole(new UserRole { UserId = UserGiangId, RoleId = RoleClinicalId, DepartmentId = DeptCdhaId });
        store.AddUserRole(new UserRole { UserId = UserHanhId, RoleId = RoleDeptAdminId, DepartmentId = DeptHcId });
        store.AddUserRole(new UserRole { UserId = UserKhanhId, RoleId = RoleNurseId, DepartmentId = DeptNoiId });
        store.AddUserRole(new UserRole { UserId = UserLinhId, RoleId = RoleNurseId, DepartmentId = DeptNgoaiId });
        store.AddUserRole(new UserRole { UserId = UserMinhId, RoleId = RoleReportId });
        store.AddUserRole(new UserRole { UserId = UserNgocId, RoleId = RoleClinicalId, DepartmentId = DeptXetNghiemId });
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
        var procId1 = Guid.NewGuid();
        var procId2 = Guid.NewGuid();
        var verId1 = Guid.NewGuid();
        var verId2 = Guid.NewGuid();

        store.AddProcedure(new ProfessionalProcedure
        {
            ProcedureId = procId1,
            ProcedureCode = "QT-NOI-001",
            Name = "Quy trình khám bệnh nội khoa",
            ProcedureType = "clinical",
            OwnerDepartmentId = DeptNoiId,
            Description = "Quy trình khám và điều trị bệnh nhân nội khoa tổng quát",
            CreatedBy = UserBinhId
        });
        store.AddProcedure(new ProfessionalProcedure
        {
            ProcedureId = procId2,
            ProcedureCode = "QT-XN-001",
            Name = "Quy trình xét nghiệm máu",
            ProcedureType = "technical",
            OwnerDepartmentId = DeptXetNghiemId,
            Description = "Quy trình lấy mẫu và phân tích xét nghiệm huyết học",
            CreatedBy = UserPhucId
        });

        store.AddProcedureVersion(new ProcedureVersion
        {
            ProcedureVersionId = verId1,
            ProcedureId = procId1,
            VersionNo = 1,
            VersionLabel = "v1.0",
            StatusCode = "published",
            DepartmentId = DeptNoiId,
            Title = "Quy trình khám nội khoa phiên bản 1.0",
            EffectiveFrom = DateTime.UtcNow.AddMonths(-6),
            CreatedBy = UserBinhId,
            PublishedBy = UserAnId,
            PublishedAt = DateTime.UtcNow.AddMonths(-6)
        });
        store.AddProcedureVersion(new ProcedureVersion
        {
            ProcedureVersionId = verId2,
            ProcedureId = procId2,
            VersionNo = 1,
            VersionLabel = "v1.0",
            StatusCode = "draft",
            DepartmentId = DeptXetNghiemId,
            Title = "Quy trình xét nghiệm máu phiên bản 1.0",
            CreatedBy = UserPhucId
        });

        store.AddProcedureStep(new ProcedureStep { ProcedureVersionId = verId1, StepNo = 1, Name = "Tiếp nhận bệnh nhân", Description = "Kiểm tra thông tin và hồ sơ bệnh án", StandardDurationMinutes = 5 });
        store.AddProcedureStep(new ProcedureStep { ProcedureVersionId = verId1, StepNo = 2, Name = "Khám lâm sàng", Description = "Thăm khám và ghi nhận triệu chứng", StandardDurationMinutes = 15 });
        store.AddProcedureStep(new ProcedureStep { ProcedureVersionId = verId1, StepNo = 3, Name = "Chỉ định cận lâm sàng", Description = "Yêu cầu xét nghiệm hoặc chẩn đoán hình ảnh nếu cần", StandardDurationMinutes = 5 });
        store.AddProcedureStep(new ProcedureStep { ProcedureVersionId = verId1, StepNo = 4, Name = "Kê đơn và tư vấn", Description = "Kê đơn thuốc và hướng dẫn điều trị", StandardDurationMinutes = 10 });

        store.AddProcedureStep(new ProcedureStep { ProcedureVersionId = verId2, StepNo = 1, Name = "Tiếp nhận phiếu chỉ định", Description = "Kiểm tra phiếu yêu cầu xét nghiệm", StandardDurationMinutes = 2 });
        store.AddProcedureStep(new ProcedureStep { ProcedureVersionId = verId2, StepNo = 2, Name = "Lấy mẫu máu", Description = "Thực hiện lấy mẫu theo quy chuẩn", StandardDurationMinutes = 5 });
        store.AddProcedureStep(new ProcedureStep { ProcedureVersionId = verId2, StepNo = 3, Name = "Phân tích mẫu", Description = "Chạy máy phân tích huyết học", StandardDurationMinutes = 30 });
        store.AddProcedureStep(new ProcedureStep { ProcedureVersionId = verId2, StepNo = 4, Name = "Trả kết quả", Description = "Xác nhận và trả kết quả cho bác sĩ chỉ định", StandardDurationMinutes = 5 });
    }

    private static void SeedTechnicalServices(MedDataStore store)
    {
        var svcId = Guid.NewGuid();
        var resId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        store.AddTechnicalService(new TechnicalService
        {
            TechnicalServiceId = svcId,
            ServiceCode = "DV-XN-CTM",
            Name = "Xét nghiệm công thức máu",
            ServiceType = "laboratory",
            DepartmentId = DeptXetNghiemId,
            Description = "Xét nghiệm huyết học cơ bản: hồng cầu, bạch cầu, tiểu cầu",
            CreatedBy = UserPhucId
        });

        store.AddResourceCatalogItem(new ResourceCatalogItem
        {
            ResourceId = resId,
            ResourceType = "consumable",
            ResourceCode = "VT-ONG-EDTA",
            Name = "Ống nghiệm EDTA",
            DefaultUnitCode = "ống"
        });

        store.AddTechnicalResourceNorm(new TechnicalResourceNorm
        {
            TechnicalServiceId = svcId,
            ResourceId = resId,
            StandardQuantity = 2,
            UnitCode = "ống",
            Note = "Mỗi lần xét nghiệm cần 2 ống EDTA"
        });

        // Phiếu chỉ định mẫu
        var patientId = Guid.NewGuid();
        store.AddPatientRef(new PatientRef
        {
            PatientRefId = patientId,
            ExternalPatientId = "BN-2024-001234",
            PatientCode = "BN001234",
            DisplayName = "Nguyễn Thị Mai",
            BirthDate = new DateOnly(1985, 3, 15),
            GenderCode = "F"
        });

        var encounterId = Guid.NewGuid();
        store.AddEncounterRef(new EncounterRef
        {
            EncounterRefId = encounterId,
            PatientRefId = patientId,
            ExternalEncounterId = "LK-2024-005678",
            EncounterType = "outpatient",
            DepartmentId = DeptNoiId,
            StartedAt = DateTime.UtcNow.AddHours(-2)
        });

        store.AddTechnicalOrder(new TechnicalOrder
        {
            TechnicalOrderId = orderId,
            TechnicalServiceId = svcId,
            PatientRefId = patientId,
            EncounterRefId = encounterId,
            OrderingDepartmentId = DeptNoiId,
            OrderedBy = UserBinhId,
            OrderStatus = "completed",
            CompletedAt = DateTime.UtcNow.AddHours(-1)
        });

        store.AddActualResourceUsage(new ActualResourceUsage
        {
            TechnicalOrderId = orderId,
            ResourceId = resId,
            ActualQuantity = 2,
            UnitCode = "ống",
            IsFinal = true,
            CapturedBy = UserPhucId
        });
    }

    private static void SeedClinicalProtocols(MedDataStore store)
    {
        var protocolId = Guid.NewGuid();
        var protocolVerId = Guid.NewGuid();

        store.AddClinicalProtocol(new ClinicalProtocol
        {
            ClinicalProtocolId = protocolId,
            ProtocolCode = "PD-NOI-THA",
            Name = "Phác đồ điều trị tăng huyết áp",
            ProtocolType = "treatment",
            OwnerDepartmentId = DeptNoiId,
            Description = "Phác đồ điều trị tăng huyết áp nguyên phát theo hướng dẫn Bộ Y tế",
            CreatedBy = UserBinhId
        });

        store.AddClinicalProtocolVersion(new ClinicalProtocolVersion
        {
            ClinicalProtocolVersionId = protocolVerId,
            ClinicalProtocolId = protocolId,
            VersionNo = 1,
            StatusCode = "published",
            Title = "Phác đồ THA v1.0 - Theo hướng dẫn 2023",
            Summary = "Điều trị bậc thang từ thay đổi lối sống đến phối hợp thuốc",
            EffectiveFrom = DateTime.UtcNow.AddMonths(-3),
            CreatedBy = UserBinhId,
            ApprovedBy = UserAnId,
            ApprovedAt = DateTime.UtcNow.AddMonths(-3),
            PublishedBy = UserAnId,
            PublishedAt = DateTime.UtcNow.AddMonths(-3)
        });

        store.AddProtocolApplicabilityRule(new ProtocolApplicabilityRule
        {
            ClinicalProtocolVersionId = protocolVerId,
            RuleType = "diagnosis",
            RuleJson = """{"icd10_codes":["I10","I11","I12","I13","I15"]}""",
            Priority = 100
        });
    }

    private static void SeedNotifications(MedDataStore store)
    {
        store.AddNotificationPreference(new NotificationPreference
        {
            UserId = UserAnId,
            NotificationType = "procedure_approval",
            ChannelCode = "in_app",
            IsEnabled = true
        });
        store.AddNotificationPreference(new NotificationPreference
        {
            UserId = UserBinhId,
            NotificationType = "procedure_approval",
            ChannelCode = "email",
            IsEnabled = true
        });

        var notifId = Guid.NewGuid();
        store.AddNotification(new MedNotification
        {
            NotificationId = notifId,
            RecipientUserId = UserAnId,
            NotificationType = "procedure_approval",
            Title = "Quy trình mới chờ phê duyệt",
            Body = "Quy trình 'Xét nghiệm máu' cần được phê duyệt bởi quản trị viên.",
            Severity = "info",
            SourceType = "procedure",
            SourceId = "QT-XN-001"
        });

        store.AddNotificationDeliveryAttempt(new NotificationDeliveryAttempt
        {
            NotificationId = notifId,
            ChannelCode = "in_app",
            DeliveryStatus = "delivered"
        });
    }
}
