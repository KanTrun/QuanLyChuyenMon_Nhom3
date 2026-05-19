using Microsoft.EntityFrameworkCore;
using TelemedicineLandingPage.Data;
using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Tests.Admin.Sql;

/// <summary>
/// Trợ giúp tạo MedDbContext in-memory cho kiểm thử đơn vị.
/// Mỗi test nhận một database riêng biệt (tên ngẫu nhiên).
/// </summary>
public static class TestDbHelper
{
    /// <summary>Tạo MedDbContext in-memory với dữ liệu seed.</summary>
    public static MedDbContext CreateSeededContext()
    {
        var options = new DbContextOptionsBuilder<MedDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var db = new MedDbContext(options);
        SeedTestData(db);
        return db;
    }

    /// <summary>Seed dữ liệu tương đương MedDataStoreSeed vào in-memory database.</summary>
    private static void SeedTestData(MedDbContext db)
    {
        // Khoa/Phòng
        db.Departments.AddRange(
            new Department { DepartmentId = MedDataStoreSeed.RootDeptId, Code = "BV-ROOT", Name = "Bệnh viện Đa khoa" },
            new Department { DepartmentId = MedDataStoreSeed.DeptNoiId, Code = "KHOA-NOI", Name = "Khoa Nội", ParentDepartmentId = MedDataStoreSeed.RootDeptId },
            new Department { DepartmentId = MedDataStoreSeed.DeptNgoaiId, Code = "KHOA-NGOAI", Name = "Khoa Ngoại", ParentDepartmentId = MedDataStoreSeed.RootDeptId },
            new Department { DepartmentId = MedDataStoreSeed.DeptXetNghiemId, Code = "KHOA-XN", Name = "Khoa Xét nghiệm", ParentDepartmentId = MedDataStoreSeed.RootDeptId }
        );
        db.DepartmentClosure.AddRange(
            new DepartmentClosureEdge { AncestorDepartmentId = MedDataStoreSeed.RootDeptId, DescendantDepartmentId = MedDataStoreSeed.RootDeptId, Depth = 0 },
            new DepartmentClosureEdge { AncestorDepartmentId = MedDataStoreSeed.DeptNoiId, DescendantDepartmentId = MedDataStoreSeed.DeptNoiId, Depth = 0 },
            new DepartmentClosureEdge { AncestorDepartmentId = MedDataStoreSeed.DeptNgoaiId, DescendantDepartmentId = MedDataStoreSeed.DeptNgoaiId, Depth = 0 },
            new DepartmentClosureEdge { AncestorDepartmentId = MedDataStoreSeed.DeptXetNghiemId, DescendantDepartmentId = MedDataStoreSeed.DeptXetNghiemId, Depth = 0 },
            new DepartmentClosureEdge { AncestorDepartmentId = MedDataStoreSeed.RootDeptId, DescendantDepartmentId = MedDataStoreSeed.DeptNoiId, Depth = 1 },
            new DepartmentClosureEdge { AncestorDepartmentId = MedDataStoreSeed.RootDeptId, DescendantDepartmentId = MedDataStoreSeed.DeptNgoaiId, Depth = 1 },
            new DepartmentClosureEdge { AncestorDepartmentId = MedDataStoreSeed.RootDeptId, DescendantDepartmentId = MedDataStoreSeed.DeptXetNghiemId, Depth = 1 }
        );

        // Vai trò
        db.Roles.AddRange(
            new Role { RoleId = MedDataStoreSeed.RoleSysAdminId, Code = "SYSTEM_ADMIN", Name = "Quản trị hệ thống", IsSystem = true },
            new Role { RoleId = MedDataStoreSeed.RoleDeptAdminId, Code = "DEPARTMENT_ADMIN", Name = "Quản trị khoa/phòng" },
            new Role { RoleId = MedDataStoreSeed.RoleClinicalId, Code = "CLINICAL_USER", Name = "Người dùng lâm sàng" },
            new Role { RoleId = MedDataStoreSeed.RoleNurseId, Code = "NURSE", Name = "Điều dưỡng" }
        );

        // Người dùng
        db.Users.Add(new AppUser
        {
            UserId = MedDataStoreSeed.AdminUserId,
            Username = "admin",
            FullName = "Quản trị viên hệ thống",
            Email = "admin@bv.vn",
            PrimaryDepartmentId = MedDataStoreSeed.RootDeptId
        });
        db.UserRoles.Add(new UserRole { UserId = MedDataStoreSeed.AdminUserId, RoleId = MedDataStoreSeed.RoleSysAdminId });

        // Màn hình
        db.Screens.AddRange(
            new ScreenCatalog { ScreenId = MedDataStoreSeed.ScreenDashId, ScreenCode = "SCR_DASHBOARD", Name = "Bảng điều khiển", Route = "/admin", ModuleCode = "CORE" },
            new ScreenCatalog { ScreenId = MedDataStoreSeed.ScreenProcId, ScreenCode = "SCR_PROCEDURES", Name = "Quản lý quy trình", Route = "/admin/procedures", ModuleCode = "PROC" },
            new ScreenCatalog { ScreenId = MedDataStoreSeed.ScreenPermId, ScreenCode = "SCR_PERMISSIONS", Name = "Quản lý phân quyền", Route = "/admin/permissions", ModuleCode = "PERM" },
            new ScreenCatalog { ScreenId = MedDataStoreSeed.ScreenOrderId, ScreenCode = "SCR_ORDERS", Name = "Chỉ định kỹ thuật", Route = "/admin/orders", ModuleCode = "TECH" }
        );

        // Quyền hạn
        db.Permissions.AddRange(
            new MedPermission { PermissionId = MedDataStoreSeed.PermViewDashId, PermissionCode = "PERM_VIEW_DASHBOARD", ScreenId = MedDataStoreSeed.ScreenDashId, ActionCode = "view" },
            new MedPermission { PermissionId = MedDataStoreSeed.PermManageProcId, PermissionCode = "PERM_MANAGE_PROC", ScreenId = MedDataStoreSeed.ScreenProcId, ActionCode = "manage" },
            new MedPermission { PermissionId = MedDataStoreSeed.PermManagePermId, PermissionCode = "PERM_MANAGE_PERM", ScreenId = MedDataStoreSeed.ScreenPermId, ActionCode = "manage" },
            new MedPermission { PermissionId = MedDataStoreSeed.PermCreateOrderId, PermissionCode = "PERM_CREATE_ORDER", ScreenId = MedDataStoreSeed.ScreenOrderId, ActionCode = "create" }
        );

        // Gán quyền cho vai trò
        db.RolePermissions.AddRange(
            new RolePermission { RoleId = MedDataStoreSeed.RoleSysAdminId, PermissionId = MedDataStoreSeed.PermViewDashId, Priority = 100 },
            new RolePermission { RoleId = MedDataStoreSeed.RoleSysAdminId, PermissionId = MedDataStoreSeed.PermManageProcId, Priority = 100 },
            new RolePermission { RoleId = MedDataStoreSeed.RoleSysAdminId, PermissionId = MedDataStoreSeed.PermManagePermId, Priority = 100 },
            new RolePermission { RoleId = MedDataStoreSeed.RoleSysAdminId, PermissionId = MedDataStoreSeed.PermCreateOrderId, Priority = 100 }
        );

        db.SaveChanges();
    }
}
