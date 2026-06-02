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
    public void Refresh()
    {
        _db.ChangeTracker.Clear();
        RaiseStateChanged();
    }

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
    public IReadOnlyList<SignatureRecord> SignatureRecords => _db.SignatureRecords.ToList();
    public IReadOnlyList<NotificationPreference> NotificationPreferences => _db.NotificationPreferences.ToList();
    public IReadOnlyList<MedNotification> Notifications => _db.Notifications.ToList();
    public IReadOnlyList<NotificationDeliveryAttempt> NotificationDeliveryAttempts => _db.NotificationDeliveryAttempts.ToList();

    // === Ghi dữ liệu — ghi trực tiếp vào SQL Server ===
    public void AddDepartment(Department dept)
    {
        var normalized = NormalizeDepartment(dept);
        ValidateDepartmentUniqueCode(normalized.DepartmentId, normalized.Code);
        ValidateDepartmentParent(normalized.DepartmentId, normalized.ParentDepartmentId);
        _db.Departments.Add(normalized);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void UpdateDepartment(Department dept)
    {
        var existing = _db.Departments.FirstOrDefault(d => d.DepartmentId == dept.DepartmentId)
            ?? throw new InvalidOperationException("Khoa/phòng không tồn tại.");
        var normalized = NormalizeDepartment(dept);
        ValidateDepartmentUniqueCode(normalized.DepartmentId, normalized.Code);
        ValidateDepartmentParent(normalized.DepartmentId, normalized.ParentDepartmentId);
        _db.Departments.Entry(existing).CurrentValues.SetValues(normalized with { UpdatedAt = DateTime.UtcNow });
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void UpdateDepartmentParent(Guid departmentId, Guid? newParentId)
    {
        var dept = _db.Departments.FirstOrDefault(d => d.DepartmentId == departmentId)
            ?? throw new InvalidOperationException("Khoa/phòng không tồn tại.");
        ValidateDepartmentParent(departmentId, newParentId);
        var updated = dept with { ParentDepartmentId = newParentId, UpdatedAt = DateTime.UtcNow };
        _db.Departments.Entry(dept).CurrentValues.SetValues(updated);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void ArchiveDepartment(Guid departmentId)
    {
        var dept = _db.Departments.FirstOrDefault(d => d.DepartmentId == departmentId)
            ?? throw new InvalidOperationException("Khoa/phòng không tồn tại.");
        var hasActiveChildren = _db.Departments.Any(d => d.ParentDepartmentId == departmentId && d.Status == "active");
        if (hasActiveChildren)
        {
            throw MedDomainException.Constraint("CK_departments_archive_children", 50021,
                "Không thể lưu trữ khoa/phòng còn đơn vị con đang hoạt động.");
        }
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

    public void AddRole(Role role)
    {
        var normalized = NormalizeRole(role);
        ValidateRoleUniqueCode(normalized.RoleId, normalized.Code);
        _db.Roles.Add(normalized);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void UpdateRole(Role role)
    {
        var existing = _db.Roles.FirstOrDefault(r => r.RoleId == role.RoleId)
            ?? throw new InvalidOperationException("Vai trò không tồn tại.");
        var normalized = NormalizeRole(role);
        ValidateRoleUniqueCode(normalized.RoleId, normalized.Code);
        _db.Roles.Entry(existing).CurrentValues.SetValues(normalized with
        {
            IsSystem = existing.IsSystem,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        });
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void ArchiveRole(Guid roleId)
    {
        var existing = _db.Roles.FirstOrDefault(r => r.RoleId == roleId)
            ?? throw new InvalidOperationException("Vai trò không tồn tại.");
        if (existing.IsSystem)
            throw new InvalidOperationException("Không thể lưu trữ vai trò hệ thống.");

        _db.Roles.Entry(existing).CurrentValues.SetValues(existing with { Status = "archived", UpdatedAt = DateTime.UtcNow });
        var activeAssignments = _db.UserRoles.Where(ur => ur.RoleId == roleId && ur.EffectiveTo == null).ToList();
        foreach (var assignment in activeAssignments)
        {
            _db.UserRoles.Entry(assignment).CurrentValues.SetValues(assignment with { EffectiveTo = DateTime.UtcNow });
        }
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void AddGroup(Group group)
    {
        var normalized = NormalizeGroup(group);
        ValidateGroupUniqueCode(normalized.GroupId, normalized.Code);
        ValidateGroupDepartment(normalized.DepartmentId);
        _db.Groups.Add(normalized);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void AddUserRole(UserRole userRole) { _db.UserRoles.Add(userRole); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddUserGroupMember(UserGroupMember member) { _db.UserGroupMembers.Add(member); _db.SaveChanges(); RaiseStateChanged(); }
    public void RemoveUserRole(Guid userRoleId)
    {
        var existing = _db.UserRoles.FirstOrDefault(r => r.UserRoleId == userRoleId)
            ?? throw new InvalidOperationException("Gán vai trò không tồn tại.");
        _db.UserRoles.Remove(existing);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void RemoveUserGroupMember(Guid membershipId)
    {
        var existing = _db.UserGroupMembers.FirstOrDefault(m => m.UserGroupMemberId == membershipId)
            ?? throw new InvalidOperationException("Thành viên nhóm không tồn tại.");
        _db.UserGroupMembers.Remove(existing);
        _db.SaveChanges();
        RaiseStateChanged();
    }

    public void AddScreen(ScreenCatalog screen) { _db.Screens.Add(screen); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddFeature(FeatureCatalog feature) { _db.Features.Add(feature); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddPermission(MedPermission permission) { _db.Permissions.Add(permission); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddRolePermission(RolePermission rp) { _db.RolePermissions.Add(rp); _db.SaveChanges(); RaiseStateChanged(); }
    public void RemoveRolePermission(Guid rolePermissionId)
    {
        var existing = _db.RolePermissions.FirstOrDefault(p => p.RolePermissionId == rolePermissionId)
            ?? throw new InvalidOperationException("Quyền vai trò không tồn tại.");
        _db.RolePermissions.Remove(existing);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void AddGroupPermission(GroupPermission gp) { _db.GroupPermissions.Add(gp); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddUserPermissionOverride(UserPermissionOverride upo) { _db.UserPermissionOverrides.Add(upo); _db.SaveChanges(); RaiseStateChanged(); }
    public void RemoveGroupPermission(Guid groupPermissionId)
    {
        var existing = _db.GroupPermissions.FirstOrDefault(p => p.GroupPermissionId == groupPermissionId)
            ?? throw new InvalidOperationException("Quyền nhóm không tồn tại.");
        _db.GroupPermissions.Remove(existing);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void UpdateUserPermissionOverride(UserPermissionOverride upo)
    {
        var existing = _db.UserPermissionOverrides.FirstOrDefault(p => p.UserPermissionOverrideId == upo.UserPermissionOverrideId)
            ?? throw new InvalidOperationException("Ghi đè quyền không tồn tại.");
        _db.UserPermissionOverrides.Entry(existing).CurrentValues.SetValues(upo);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void RemoveUserPermissionOverride(Guid userPermissionOverrideId)
    {
        var existing = _db.UserPermissionOverrides.FirstOrDefault(p => p.UserPermissionOverrideId == userPermissionOverrideId)
            ?? throw new InvalidOperationException("Ghi đè quyền không tồn tại.");
        _db.UserPermissionOverrides.Remove(existing);
        _db.SaveChanges();
        RaiseStateChanged();
    }

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
    public void UpdateProcedure(ProfessionalProcedure proc)
    {
        var existing = _db.Procedures.FirstOrDefault(p => p.ProcedureId == proc.ProcedureId)
            ?? throw new InvalidOperationException("Quy trình không tồn tại.");
        _db.Procedures.Entry(existing).CurrentValues.SetValues(proc with
        {
            CreatedAt = existing.CreatedAt,
            CreatedBy = existing.CreatedBy,
            UpdatedAt = DateTime.UtcNow
        });
        _db.SaveChanges();
        RaiseStateChanged();
    }

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
    public void RemoveProcedureAttachment(Guid attachmentId)
    {
        var existing = _db.ProcedureAttachments.FirstOrDefault(a => a.ProcedureAttachmentId == attachmentId)
            ?? throw new InvalidOperationException("Tệp đính kèm không tồn tại.");
        _db.ProcedureAttachments.Remove(existing);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void RemoveProcedureScreenMapping(Guid mappingId)
    {
        var existing = _db.ProcedureScreenMappings.FirstOrDefault(m => m.ProcedureScreenMappingId == mappingId)
            ?? throw new InvalidOperationException("Ánh xạ màn hình không tồn tại.");
        _db.ProcedureScreenMappings.Remove(existing);
        _db.SaveChanges();
        RaiseStateChanged();
    }

    public void AddPatientRef(PatientRef patient) { _db.PatientRefs.Add(patient); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddEncounterRef(EncounterRef encounter) { _db.EncounterRefs.Add(encounter); _db.SaveChanges(); RaiseStateChanged(); }
    public void UpdatePatientRef(PatientRef patient)
    {
        var existing = _db.PatientRefs.FirstOrDefault(p => p.PatientRefId == patient.PatientRefId)
            ?? throw new InvalidOperationException("Bệnh nhân không tồn tại.");
        _db.PatientRefs.Entry(existing).CurrentValues.SetValues(patient);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void UpdateEncounterRef(EncounterRef encounter)
    {
        var existing = _db.EncounterRefs.FirstOrDefault(e => e.EncounterRefId == encounter.EncounterRefId)
            ?? throw new InvalidOperationException("Lượt khám không tồn tại.");
        _db.EncounterRefs.Entry(existing).CurrentValues.SetValues(encounter);
        _db.SaveChanges();
        RaiseStateChanged();
    }

    public void AddTechnicalService(TechnicalService svc) { _db.TechnicalServices.Add(svc); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddResourceCatalogItem(ResourceCatalogItem item) { _db.ResourceCatalog.Add(item); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddTechnicalResourceNorm(TechnicalResourceNorm norm) { _db.TechnicalResourceNorms.Add(norm); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddProcedureVersionResourceNorm(ProcedureVersionResourceNorm norm) { _db.ProcedureVersionResourceNorms.Add(norm); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddTechnicalOrder(TechnicalOrder order) { _db.TechnicalOrders.Add(order); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddResourceAvailabilitySnapshot(ResourceAvailabilitySnapshot snap) { _db.ResourceAvailabilitySnapshots.Add(snap); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddActualResourceUsage(ActualResourceUsage usage) { _db.ActualResourceUsages.Add(usage); _db.SaveChanges(); RaiseStateChanged(); }
    public void UpdateTechnicalService(TechnicalService svc)
    {
        var existing = _db.TechnicalServices.FirstOrDefault(s => s.TechnicalServiceId == svc.TechnicalServiceId)
            ?? throw new InvalidOperationException("Dịch vụ kỹ thuật không tồn tại.");
        _db.TechnicalServices.Entry(existing).CurrentValues.SetValues(svc);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void RemoveTechnicalService(Guid technicalServiceId)
    {
        var existing = _db.TechnicalServices.FirstOrDefault(s => s.TechnicalServiceId == technicalServiceId)
            ?? throw new InvalidOperationException("Dịch vụ kỹ thuật không tồn tại.");
        _db.TechnicalServices.Entry(existing).CurrentValues.SetValues(existing with { Status = "archived", UpdatedAt = DateTime.UtcNow });
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void UpdateResourceCatalogItem(ResourceCatalogItem item)
    {
        var existing = _db.ResourceCatalog.FirstOrDefault(r => r.ResourceId == item.ResourceId)
            ?? throw new InvalidOperationException("Tài nguyên không tồn tại.");
        _db.ResourceCatalog.Entry(existing).CurrentValues.SetValues(item);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void RemoveResourceCatalogItem(Guid resourceId)
    {
        var existing = _db.ResourceCatalog.FirstOrDefault(r => r.ResourceId == resourceId)
            ?? throw new InvalidOperationException("Tài nguyên không tồn tại.");
        _db.ResourceCatalog.Entry(existing).CurrentValues.SetValues(existing with { Status = "archived", UpdatedAt = DateTime.UtcNow });
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void RemoveTechnicalResourceNorm(Guid normId)
    {
        var existing = _db.TechnicalResourceNorms.FirstOrDefault(n => n.TechnicalResourceNormId == normId)
            ?? throw new InvalidOperationException("Định mức dịch vụ không tồn tại.");
        _db.TechnicalResourceNorms.Remove(existing);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void RemoveProcedureVersionResourceNorm(Guid normId)
    {
        var existing = _db.ProcedureVersionResourceNorms.FirstOrDefault(n => n.ProcedureVersionResourceNormId == normId)
            ?? throw new InvalidOperationException("Định mức phiên bản quy trình không tồn tại.");
        _db.ProcedureVersionResourceNorms.Remove(existing);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void UpdateTechnicalOrder(TechnicalOrder order)
    {
        var existing = _db.TechnicalOrders.FirstOrDefault(o => o.TechnicalOrderId == order.TechnicalOrderId)
            ?? throw new InvalidOperationException("Chỉ định kỹ thuật không tồn tại.");
        _db.TechnicalOrders.Entry(existing).CurrentValues.SetValues(order);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void RemoveActualResourceUsage(Guid usageId)
    {
        var existing = _db.ActualResourceUsages.FirstOrDefault(u => u.ActualResourceUsageId == usageId)
            ?? throw new InvalidOperationException("Ghi nhận sử dụng thực tế không tồn tại.");
        _db.ActualResourceUsages.Remove(existing);
        _db.SaveChanges();
        RaiseStateChanged();
    }

    public void AddClinicalProtocol(ClinicalProtocol protocol) { _db.ClinicalProtocols.Add(protocol); _db.SaveChanges(); RaiseStateChanged(); }
    public void UpdateClinicalProtocol(ClinicalProtocol protocol)
    {
        var existing = _db.ClinicalProtocols.FirstOrDefault(p => p.ClinicalProtocolId == protocol.ClinicalProtocolId)
            ?? throw new InvalidOperationException("Phác đồ không tồn tại.");
        _db.ClinicalProtocols.Entry(existing).CurrentValues.SetValues(protocol with
        {
            CreatedAt = existing.CreatedAt,
            CreatedBy = existing.CreatedBy,
            UpdatedAt = DateTime.UtcNow
        });
        _db.SaveChanges();
        RaiseStateChanged();
    }

    public void AddClinicalProtocolVersion(ClinicalProtocolVersion ver) { _db.ClinicalProtocolVersions.Add(ver); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddClinicalProtocolProcedure(ClinicalProtocolProcedure cpp) { _db.ClinicalProtocolProcedures.Add(cpp); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddProtocolApplicabilityRule(ProtocolApplicabilityRule rule) { _db.ProtocolApplicabilityRules.Add(rule); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddPatientProtocolApplication(PatientProtocolApplication app) { _db.PatientProtocolApplications.Add(app); _db.SaveChanges(); RaiseStateChanged(); }
    public void UpdatePatientProtocolApplication(PatientProtocolApplication app)
    {
        var existing = _db.PatientProtocolApplications.FirstOrDefault(a => a.PatientProtocolApplicationId == app.PatientProtocolApplicationId)
            ?? throw new InvalidOperationException("Áp dụng phác đồ không tồn tại.");
        _db.PatientProtocolApplications.Entry(existing).CurrentValues.SetValues(app);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void UpdateClinicalProtocolVersion(ClinicalProtocolVersion ver)
    {
        var existing = _db.ClinicalProtocolVersions.FirstOrDefault(v => v.ClinicalProtocolVersionId == ver.ClinicalProtocolVersionId)
            ?? throw new InvalidOperationException("Phiên bản phác đồ không tồn tại.");
        _db.ClinicalProtocolVersions.Entry(existing).CurrentValues.SetValues(ver);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void RemoveClinicalProtocolProcedure(Guid clinicalProtocolProcedureId)
    {
        var existing = _db.ClinicalProtocolProcedures.FirstOrDefault(p => p.ClinicalProtocolProcedureId == clinicalProtocolProcedureId)
            ?? throw new InvalidOperationException("Liên kết phác đồ - quy trình không tồn tại.");
        _db.ClinicalProtocolProcedures.Remove(existing);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void RemoveProtocolApplicabilityRule(Guid ruleId)
    {
        var existing = _db.ProtocolApplicabilityRules.FirstOrDefault(r => r.ProtocolApplicabilityRuleId == ruleId)
            ?? throw new InvalidOperationException("Quy tắc áp dụng không tồn tại.");
        _db.ProtocolApplicabilityRules.Remove(existing);
        _db.SaveChanges();
        RaiseStateChanged();
    }

    public void AddSignatureRecord(SignatureRecord signature) { _db.SignatureRecords.Add(signature); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddNotificationPreference(NotificationPreference pref) { _db.NotificationPreferences.Add(pref); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddNotification(MedNotification notification) { _db.Notifications.Add(notification); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddNotificationDeliveryAttempt(NotificationDeliveryAttempt attempt) { _db.NotificationDeliveryAttempts.Add(attempt); _db.SaveChanges(); RaiseStateChanged(); }
    public void UpdateNotificationPreference(NotificationPreference pref)
    {
        var existing = _db.NotificationPreferences.FirstOrDefault(p => p.NotificationPreferenceId == pref.NotificationPreferenceId)
            ?? throw new InvalidOperationException("Cài đặt thông báo không tồn tại.");
        _db.NotificationPreferences.Entry(existing).CurrentValues.SetValues(pref);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void RemoveNotificationPreference(Guid prefId)
    {
        var existing = _db.NotificationPreferences.FirstOrDefault(p => p.NotificationPreferenceId == prefId)
            ?? throw new InvalidOperationException("Cài đặt thông báo không tồn tại.");
        _db.NotificationPreferences.Remove(existing);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void UpdateNotificationReadAt(Guid notificationId, DateTime readAt)
    {
        var existing = _db.Notifications.FirstOrDefault(n => n.NotificationId == notificationId)
            ?? throw new InvalidOperationException("Thông báo không tồn tại.");
        _db.Notifications.Entry(existing).CurrentValues.SetValues(existing with { ReadAt = readAt });
        _db.SaveChanges();
        RaiseStateChanged();
    }

    private Department NormalizeDepartment(Department dept)
        => dept with
        {
            Code = dept.Code.Trim().ToUpperInvariant(),
            Name = dept.Name.Trim(),
            ParentDepartmentId = dept.ParentDepartmentId == Guid.Empty ? null : dept.ParentDepartmentId
        };

    private void ValidateDepartmentUniqueCode(Guid departmentId, string code)
    {
        if (_db.Departments.Any(d => d.DepartmentId != departmentId && d.Code.ToUpper() == code.ToUpper()))
        {
            throw MedDomainException.Constraint("UQ_departments_code", 2627,
                "Mã khoa/phòng đã tồn tại. Vui lòng dùng mã khác.");
        }
    }

    private void ValidateDepartmentParent(Guid departmentId, Guid? parentDepartmentId)
    {
        if (parentDepartmentId is null)
        {
            return;
        }

        if (parentDepartmentId == departmentId)
        {
            throw MedDomainException.Constraint("CK_departments_parent_not_self", 50020,
                "Khoa/phòng không thể là đơn vị cha của chính nó.");
        }

        var parentExists = _db.Departments.Any(d => d.DepartmentId == parentDepartmentId && d.Status == "active");
        if (!parentExists)
        {
            throw MedDomainException.Constraint("FK_departments_parent", 547,
                "Đơn vị cha không tồn tại hoặc đã lưu trữ.");
        }

        var parentIsDescendant = _db.DepartmentClosure.Any(e =>
            e.AncestorDepartmentId == departmentId &&
            e.DescendantDepartmentId == parentDepartmentId);
        if (parentIsDescendant)
        {
            throw MedDomainException.Constraint("TR_department_closure_cycle", 50020,
                "Không thể chuyển vào đơn vị con vì sẽ tạo vòng lặp tổ chức.");
        }
    }

    private static Role NormalizeRole(Role role)
        => role with
        {
            Code = role.Code.Trim().ToUpperInvariant(),
            Name = role.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(role.Description) ? null : role.Description.Trim()
        };

    private void ValidateRoleUniqueCode(Guid roleId, string code)
    {
        if (_db.Roles.Any(r => r.RoleId != roleId && r.Code.ToUpper() == code.ToUpper()))
        {
            throw MedDomainException.Constraint("UQ_roles_code", 2627,
                "Mã vai trò đã tồn tại. Vui lòng dùng mã khác.");
        }
    }

    private static Group NormalizeGroup(Group group)
        => group with
        {
            Code = group.Code.Trim().ToUpperInvariant(),
            Name = group.Name.Trim(),
            DepartmentId = group.DepartmentId == Guid.Empty ? null : group.DepartmentId,
            Description = string.IsNullOrWhiteSpace(group.Description) ? null : group.Description.Trim()
        };

    private void ValidateGroupUniqueCode(Guid groupId, string code)
    {
        if (_db.Groups.Any(g => g.GroupId != groupId && g.Code.ToUpper() == code.ToUpper()))
        {
            throw MedDomainException.Constraint("UQ_groups_code", 2627,
                "Mã nhóm đã tồn tại. Vui lòng dùng mã khác.");
        }
    }

    private void ValidateGroupDepartment(Guid? departmentId)
    {
        if (departmentId is null)
        {
            return;
        }

        var departmentExists = _db.Departments.Any(d => d.DepartmentId == departmentId && d.Status == "active");
        if (!departmentExists)
        {
            throw MedDomainException.Constraint("FK_groups_department", 547,
                "Khoa/phòng của nhóm không tồn tại hoặc đã lưu trữ.");
        }
    }
}
