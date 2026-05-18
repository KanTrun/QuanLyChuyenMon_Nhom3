using TelemedicineLandingPage.Data;
using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>
/// Triển khai IMedDataStore dựa trên MedDbContext (SQL Server thật).
/// Thay thế MedDataStore in-memory — tất cả Razor pages hiện tại tiếp tục hoạt động.
/// </summary>
public sealed class MedDbDataStore : IMedDataStore
{
    private readonly MedDbContext _db;

    public MedDbDataStore(MedDbContext db)
    {
        _db = db;
    }

    // Sự kiện thay đổi trạng thái (giữ tương thích giao diện)
    public event Action? StateChanged;
    private void RaiseStateChanged() => StateChanged?.Invoke();

    // === Đọc dữ liệu — truy vấn trực tiếp từ SQL Server ===
    public IReadOnlyList<Department> Departments => _db.Departments.ToList();
    public IReadOnlyList<DepartmentClosureEdge> DepartmentClosure => _db.DepartmentClosure.ToList();
    public IReadOnlyList<AppUser> Users => _db.Users.ToList();
    public IReadOnlyList<Role> Roles => _db.Roles.ToList();
    public IReadOnlyList<Group> Groups => _db.Groups.ToList();
    public IReadOnlyList<UserRole> UserRoles => _db.UserRoles.ToList();
    public IReadOnlyList<UserGroupMember> UserGroupMembers => _db.UserGroupMembers.ToList();
    public IReadOnlyList<ScreenCatalog> Screens => _db.Screens.ToList();
    public IReadOnlyList<FeatureCatalog> Features => _db.Features.ToList();
    public IReadOnlyList<MedPermission> Permissions => _db.Permissions.ToList();
    public IReadOnlyList<RolePermission> RolePermissions => _db.RolePermissions.ToList();
    public IReadOnlyList<GroupPermission> GroupPermissions => _db.GroupPermissions.ToList();
    public IReadOnlyList<UserPermissionOverride> UserPermissionOverrides => _db.UserPermissionOverrides.ToList();
    public IReadOnlyList<AuditLog> AuditLogs => _db.AuditLogs.OrderByDescending(a => a.OccurredAt).ToList();
    public IReadOnlyList<PermissionChangeRequest> PermissionChangeRequests => _db.PermissionChangeRequests.ToList();
    public IReadOnlyList<PermissionChangeItem> PermissionChangeItems => _db.PermissionChangeItems.ToList();
    public IReadOnlyList<ProfessionalProcedure> Procedures => _db.Procedures.ToList();
    public IReadOnlyList<ProcedureVersion> ProcedureVersions => _db.ProcedureVersions.ToList();
    public IReadOnlyList<ProcedureStep> ProcedureSteps => _db.ProcedureSteps.ToList();
    public IReadOnlyList<ProcedureAttachment> ProcedureAttachments => _db.ProcedureAttachments.ToList();
    public IReadOnlyList<ProcedureScreenMapping> ProcedureScreenMappings => _db.ProcedureScreenMappings.ToList();
    public IReadOnlyList<PatientRef> PatientRefs => _db.PatientRefs.ToList();
    public IReadOnlyList<EncounterRef> EncounterRefs => _db.EncounterRefs.ToList();
    public IReadOnlyList<TechnicalService> TechnicalServices => _db.TechnicalServices.ToList();
    public IReadOnlyList<ResourceCatalogItem> ResourceCatalog => _db.ResourceCatalog.ToList();
    public IReadOnlyList<TechnicalResourceNorm> TechnicalResourceNorms => _db.TechnicalResourceNorms.ToList();
    public IReadOnlyList<ProcedureVersionResourceNorm> ProcedureVersionResourceNorms => _db.ProcedureVersionResourceNorms.ToList();
    public IReadOnlyList<TechnicalOrder> TechnicalOrders => _db.TechnicalOrders.ToList();
    public IReadOnlyList<ResourceAvailabilitySnapshot> ResourceAvailabilitySnapshots => _db.ResourceAvailabilitySnapshots.ToList();
    public IReadOnlyList<ActualResourceUsage> ActualResourceUsages => _db.ActualResourceUsages.ToList();
    public IReadOnlyList<ClinicalProtocol> ClinicalProtocols => _db.ClinicalProtocols.ToList();
    public IReadOnlyList<ClinicalProtocolVersion> ClinicalProtocolVersions => _db.ClinicalProtocolVersions.ToList();
    public IReadOnlyList<ClinicalProtocolProcedure> ClinicalProtocolProcedures => _db.ClinicalProtocolProcedures.ToList();
    public IReadOnlyList<ProtocolApplicabilityRule> ProtocolApplicabilityRules => _db.ProtocolApplicabilityRules.ToList();
    public IReadOnlyList<PatientProtocolApplication> PatientProtocolApplications => _db.PatientProtocolApplications.ToList();
    public IReadOnlyList<NotificationPreference> NotificationPreferences => _db.NotificationPreferences.ToList();
    public IReadOnlyList<MedNotification> Notifications => _db.Notifications.ToList();
    public IReadOnlyList<NotificationDeliveryAttempt> NotificationDeliveryAttempts => _db.NotificationDeliveryAttempts.ToList();

    // === Ghi dữ liệu — ghi trực tiếp vào SQL Server ===
    public void AddDepartment(Department dept) { _db.Departments.Add(dept); _db.SaveChanges(); RaiseStateChanged(); }
    public void UpdateDepartmentParent(Guid departmentId, Guid? newParentId)
    {
        var dept = _db.Departments.FirstOrDefault(d => d.DepartmentId == departmentId)
            ?? throw new InvalidOperationException("Khoa/phòng không tồn tại.");
        var updated = dept with { ParentDepartmentId = newParentId, UpdatedAt = DateTime.UtcNow };
        _db.Departments.Entry(dept).CurrentValues.SetValues(updated);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void ArchiveDepartment(Guid departmentId)
    {
        var dept = _db.Departments.FirstOrDefault(d => d.DepartmentId == departmentId)
            ?? throw new InvalidOperationException("Khoa/phòng không tồn tại.");
        var updated = dept with { Status = "archived", UpdatedAt = DateTime.UtcNow };
        _db.Departments.Entry(dept).CurrentValues.SetValues(updated);
        _db.SaveChanges();
        RaiseStateChanged();
    }

    public void AddUser(AppUser user) { _db.Users.Add(user); _db.SaveChanges(); RaiseStateChanged(); }
    public void UpdateUser(AppUser user)
    {
        var existing = _db.Users.FirstOrDefault(u => u.UserId == user.UserId);
        if (existing is not null) _db.Users.Entry(existing).CurrentValues.SetValues(user);
        _db.SaveChanges();
        RaiseStateChanged();
    }

    public void AddRole(Role role) { _db.Roles.Add(role); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddGroup(Group group) { _db.Groups.Add(group); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddUserRole(UserRole userRole) { _db.UserRoles.Add(userRole); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddUserGroupMember(UserGroupMember member) { _db.UserGroupMembers.Add(member); _db.SaveChanges(); RaiseStateChanged(); }

    public void AddScreen(ScreenCatalog screen) { _db.Screens.Add(screen); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddFeature(FeatureCatalog feature) { _db.Features.Add(feature); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddPermission(MedPermission permission) { _db.Permissions.Add(permission); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddRolePermission(RolePermission rp) { _db.RolePermissions.Add(rp); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddGroupPermission(GroupPermission gp) { _db.GroupPermissions.Add(gp); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddUserPermissionOverride(UserPermissionOverride upo) { _db.UserPermissionOverrides.Add(upo); _db.SaveChanges(); RaiseStateChanged(); }

    public void AppendAudit(AuditLog log) { _db.AuditLogs.Add(log); _db.SaveChanges(); }

    public void AddPermissionChangeRequest(PermissionChangeRequest req) { _db.PermissionChangeRequests.Add(req); _db.SaveChanges(); RaiseStateChanged(); }
    public void UpdatePermissionChangeRequest(PermissionChangeRequest updated)
    {
        var existing = _db.PermissionChangeRequests.FirstOrDefault(r => r.PermissionChangeRequestId == updated.PermissionChangeRequestId);
        if (existing is not null) _db.PermissionChangeRequests.Entry(existing).CurrentValues.SetValues(updated);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void AddPermissionChangeItem(PermissionChangeItem item) { _db.PermissionChangeItems.Add(item); _db.SaveChanges(); RaiseStateChanged(); }

    public void AddProcedure(ProfessionalProcedure proc) { _db.Procedures.Add(proc); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddProcedureVersion(ProcedureVersion ver) { _db.ProcedureVersions.Add(ver); _db.SaveChanges(); RaiseStateChanged(); }
    public void UpdateProcedureVersion(ProcedureVersion updated)
    {
        var existing = _db.ProcedureVersions.FirstOrDefault(v => v.ProcedureVersionId == updated.ProcedureVersionId);
        if (existing is not null) _db.ProcedureVersions.Entry(existing).CurrentValues.SetValues(updated);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void AddProcedureStep(ProcedureStep step) { _db.ProcedureSteps.Add(step); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddProcedureAttachment(ProcedureAttachment att) { _db.ProcedureAttachments.Add(att); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddProcedureScreenMapping(ProcedureScreenMapping mapping) { _db.ProcedureScreenMappings.Add(mapping); _db.SaveChanges(); RaiseStateChanged(); }

    public void AddPatientRef(PatientRef patient) { _db.PatientRefs.Add(patient); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddEncounterRef(EncounterRef encounter) { _db.EncounterRefs.Add(encounter); _db.SaveChanges(); RaiseStateChanged(); }

    public void AddTechnicalService(TechnicalService svc) { _db.TechnicalServices.Add(svc); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddResourceCatalogItem(ResourceCatalogItem item) { _db.ResourceCatalog.Add(item); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddTechnicalResourceNorm(TechnicalResourceNorm norm) { _db.TechnicalResourceNorms.Add(norm); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddProcedureVersionResourceNorm(ProcedureVersionResourceNorm norm) { _db.ProcedureVersionResourceNorms.Add(norm); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddTechnicalOrder(TechnicalOrder order) { _db.TechnicalOrders.Add(order); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddResourceAvailabilitySnapshot(ResourceAvailabilitySnapshot snap) { _db.ResourceAvailabilitySnapshots.Add(snap); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddActualResourceUsage(ActualResourceUsage usage) { _db.ActualResourceUsages.Add(usage); _db.SaveChanges(); RaiseStateChanged(); }

    public void AddClinicalProtocol(ClinicalProtocol protocol) { _db.ClinicalProtocols.Add(protocol); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddClinicalProtocolVersion(ClinicalProtocolVersion ver) { _db.ClinicalProtocolVersions.Add(ver); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddClinicalProtocolProcedure(ClinicalProtocolProcedure cpp) { _db.ClinicalProtocolProcedures.Add(cpp); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddProtocolApplicabilityRule(ProtocolApplicabilityRule rule) { _db.ProtocolApplicabilityRules.Add(rule); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddPatientProtocolApplication(PatientProtocolApplication app) { _db.PatientProtocolApplications.Add(app); _db.SaveChanges(); RaiseStateChanged(); }

    public void AddNotificationPreference(NotificationPreference pref) { _db.NotificationPreferences.Add(pref); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddNotification(MedNotification notification) { _db.Notifications.Add(notification); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddNotificationDeliveryAttempt(NotificationDeliveryAttempt attempt) { _db.NotificationDeliveryAttempts.Add(attempt); _db.SaveChanges(); RaiseStateChanged(); }
}
