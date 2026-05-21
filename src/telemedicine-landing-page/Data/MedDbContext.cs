using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Text.Json;
using TelemedicineLandingPage.Models.Auth;
using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Data;

/// <summary>
/// DbContext kết nối SQL Server — schema med.
/// Ánh xạ toàn bộ bảng trong cơ sở dữ liệu MedicalProcedureManagement.
/// </summary>
public class MedDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    private static readonly JsonSerializerOptions AuditJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public MedDbContext(DbContextOptions<MedDbContext> options) : base(options) { }

    // === Tổ chức & Danh tính ===
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<DepartmentClosureEdge> DepartmentClosure => Set<DepartmentClosureEdge>();
    public new DbSet<AppUser> Users => Set<AppUser>();
    public new DbSet<Role> Roles => Set<Role>();
    public DbSet<Group> Groups => Set<Group>();
    public new DbSet<UserRole> UserRoles => Set<UserRole>();
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

        modelBuilder.Entity<ApplicationUser>().ToTable("identity_users", "auth");
        modelBuilder.Entity<ApplicationRole>().ToTable("identity_roles", "auth");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("identity_user_roles", "auth");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("identity_user_claims", "auth");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("identity_user_logins", "auth");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("identity_role_claims", "auth");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("identity_user_tokens", "auth");
        modelBuilder.Entity<IdentityUserLogin<Guid>>(entity =>
        {
            entity.Property(e => e.LoginProvider).HasMaxLength(128);
            entity.Property(e => e.ProviderKey).HasMaxLength(128);
        });
        modelBuilder.Entity<IdentityUserToken<Guid>>(entity =>
        {
            entity.Property(e => e.LoginProvider).HasMaxLength(128);
            entity.Property(e => e.Name).HasMaxLength(128);
        });

        // DepartmentClosureEdge: composite key
        modelBuilder.Entity<DepartmentClosureEdge>()
            .HasKey(e => new { e.AncestorDepartmentId, e.DescendantDepartmentId });

        // SQL Server disables EF Core's OUTPUT clause for tables with triggers.
        modelBuilder.Entity<Department>()
            .ToTable("departments", "med", table => table.HasTrigger("TR_departments_insert_closure"));

        modelBuilder.Entity<AppUser>()
            .ToTable("users", "med", table => table.HasTrigger("TR_users_expire_security_assignments"));

        modelBuilder.Entity<AuditLog>()
            .ToTable("audit_logs", "med", table => table.HasTrigger("TR_audit_logs_immutable"));

        modelBuilder.Entity<ActualResourceUsage>()
            .ToTable("actual_resource_usages", "med", table => table.HasTrigger("TR_actual_resource_usages_set_final"));
    }

    public override int SaveChanges()
    {
        AddAutomaticAuditLogs();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AddAutomaticAuditLogs();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void AddAutomaticAuditLogs()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is not AuditLog &&
                        e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            var tableName = entry.Metadata.GetTableName() ?? entry.Metadata.ClrType.Name;
            var targetType = NormalizeTargetType(tableName);
            var targetId = GetPrimaryKey(entry);

            AuditLogs.Add(new AuditLog
            {
                CorrelationId = Guid.NewGuid(),
                ActionCode = entry.State switch
                {
                    EntityState.Added => "create",
                    EntityState.Modified => "update",
                    EntityState.Deleted => "delete",
                    _ => "update"
                },
                TargetType = targetType,
                TargetId = targetId,
                DepartmentId = TryGetDepartmentId(entry),
                BeforeJson = entry.State is EntityState.Modified or EntityState.Deleted
                    ? ToAuditJson(entry.Properties.ToDictionary(p => p.Metadata.GetColumnName(), p => p.OriginalValue))
                    : null,
                AfterJson = entry.State is EntityState.Added or EntityState.Modified
                    ? ToAuditJson(entry.Properties.ToDictionary(p => p.Metadata.GetColumnName(), p => p.CurrentValue))
                    : null,
                MetadataJson = ToAuditJson(new
                {
                    entity = entry.Metadata.ClrType.Name,
                    table = tableName,
                    automatic = true
                })
            });
        }
    }

    private static string NormalizeTargetType(string tableName)
    {
        if (tableName.EndsWith("ies", StringComparison.OrdinalIgnoreCase))
            return ToSnakeCase(tableName[..^3] + "y");
        if (tableName.EndsWith("ses", StringComparison.OrdinalIgnoreCase))
            return ToSnakeCase(tableName[..^2]);
        if (tableName.EndsWith("s", StringComparison.OrdinalIgnoreCase))
            return ToSnakeCase(tableName[..^1]);
        return ToSnakeCase(tableName);
    }

    private static string ToSnakeCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;

        var chars = new List<char>(value.Length + 8);
        for (var i = 0; i < value.Length; i++)
        {
            var ch = value[i];
            if (char.IsUpper(ch))
            {
                if (i > 0 && chars[^1] != '_')
                    chars.Add('_');
                chars.Add(char.ToLowerInvariant(ch));
            }
            else
            {
                chars.Add(ch);
            }
        }

        return new string(chars.ToArray());
    }

    private static string? GetPrimaryKey(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null) return null;

        var values = key.Properties
            .Select(p => entry.Property(p.Name).CurrentValue?.ToString())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();

        return values.Count == 0 ? null : string.Join(":", values);
    }

    private static Guid? TryGetDepartmentId(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var departmentProperty = entry.Properties.FirstOrDefault(p =>
            string.Equals(p.Metadata.Name, "DepartmentId", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(p.Metadata.Name, "OwnerDepartmentId", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(p.Metadata.Name, "PrimaryDepartmentId", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(p.Metadata.Name, "OrderingDepartmentId", StringComparison.OrdinalIgnoreCase));

        return departmentProperty?.CurrentValue as Guid?;
    }

    private static string ToAuditJson(object value) => JsonSerializer.Serialize(value, AuditJsonOptions);
}
