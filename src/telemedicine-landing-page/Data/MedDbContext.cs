using Microsoft.EntityFrameworkCore;
using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Data;

/// <summary>
/// DbContext kết nối SQL Server — schema med.
/// Ánh xạ toàn bộ bảng trong cơ sở dữ liệu MedicalProcedureManagement.
/// </summary>
public class MedDbContext : DbContext
{
    public MedDbContext(DbContextOptions<MedDbContext> options) : base(options) { }

    // === Tổ chức & Danh tính ===
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<DepartmentClosureEdge> DepartmentClosure => Set<DepartmentClosureEdge>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserGroupMember> UserGroupMembers => Set<UserGroupMember>();

    // === Quyền hạn ===
    public DbSet<ScreenCatalog> Screens => Set<ScreenCatalog>();
    public DbSet<FeatureCatalog> Features => Set<FeatureCatalog>();
    public DbSet<MedPermission> Permissions => Set<MedPermission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<GroupPermission> GroupPermissions => Set<GroupPermission>();
    public DbSet<UserPermissionOverride> UserPermissionOverrides => Set<UserPermissionOverride>();

    // === Nhật ký kiểm toán ===
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // === Yêu cầu thay đổi quyền ===
    public DbSet<PermissionChangeRequest> PermissionChangeRequests => Set<PermissionChangeRequest>();
    public DbSet<PermissionChangeItem> PermissionChangeItems => Set<PermissionChangeItem>();

    // === Quy trình chuyên môn ===
    public DbSet<ProfessionalProcedure> Procedures => Set<ProfessionalProcedure>();
    public DbSet<ProcedureVersion> ProcedureVersions => Set<ProcedureVersion>();
    public DbSet<ProcedureStep> ProcedureSteps => Set<ProcedureStep>();
    public DbSet<ProcedureAttachment> ProcedureAttachments => Set<ProcedureAttachment>();
    public DbSet<ProcedureScreenMapping> ProcedureScreenMappings => Set<ProcedureScreenMapping>();

    // === Bệnh nhân & Lượt khám ===
    public DbSet<PatientRef> PatientRefs => Set<PatientRef>();
    public DbSet<EncounterRef> EncounterRefs => Set<EncounterRef>();

    // === Dịch vụ kỹ thuật & Nguồn lực ===
    public DbSet<TechnicalService> TechnicalServices => Set<TechnicalService>();
    public DbSet<ResourceCatalogItem> ResourceCatalog => Set<ResourceCatalogItem>();
    public DbSet<TechnicalResourceNorm> TechnicalResourceNorms => Set<TechnicalResourceNorm>();
    public DbSet<ProcedureVersionResourceNorm> ProcedureVersionResourceNorms => Set<ProcedureVersionResourceNorm>();
    public DbSet<TechnicalOrder> TechnicalOrders => Set<TechnicalOrder>();
    public DbSet<ResourceAvailabilitySnapshot> ResourceAvailabilitySnapshots => Set<ResourceAvailabilitySnapshot>();
    public DbSet<ActualResourceUsage> ActualResourceUsages => Set<ActualResourceUsage>();

    // === Phác đồ lâm sàng ===
    public DbSet<ClinicalProtocol> ClinicalProtocols => Set<ClinicalProtocol>();
    public DbSet<ClinicalProtocolVersion> ClinicalProtocolVersions => Set<ClinicalProtocolVersion>();
    public DbSet<ClinicalProtocolProcedure> ClinicalProtocolProcedures => Set<ClinicalProtocolProcedure>();
    public DbSet<ProtocolApplicabilityRule> ProtocolApplicabilityRules => Set<ProtocolApplicabilityRule>();
    public DbSet<PatientProtocolApplication> PatientProtocolApplications => Set<PatientProtocolApplication>();

    // === Thông báo ===
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<MedNotification> Notifications => Set<MedNotification>();
    public DbSet<NotificationDeliveryAttempt> NotificationDeliveryAttempts => Set<NotificationDeliveryAttempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // DepartmentClosureEdge: composite key
        modelBuilder.Entity<DepartmentClosureEdge>()
            .HasKey(e => new { e.AncestorDepartmentId, e.DescendantDepartmentId });
    }
}
